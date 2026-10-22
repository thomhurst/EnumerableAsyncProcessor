# WorktreeCleanup.ps1
# Shared worktree-removal helper, dot-sourced by Merge-Pr.ps1 and
# Remove-MergedWorktrees.ps1. Not meant to be run directly.
#
# Removal policy (one place, both callers):
#   - PRESERVE a worktree with uncommitted TRACKED changes (real WIP). Never
#     force-discard it; the caller logs and moves on.
#   - CLEAR untracked build artifacts (node_modules/bin/obj) — they are not work.
#   - RETRY removals that a busy directory interrupted (see PENDING REMOVALS).
#   - DETACH directory junctions and symlinks before deleting, so no delete follows a link.
#   - Long-path safe: git's own delete now works because core.longpaths=true is set
#     system-wide; the `\\?\` extended-length Remove-Item is kept as a fallback for
#     environments where that config is missing.

function Remove-MergedWorktree {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Repo,       # a checkout that is NOT the one being removed (main)
        [Parameter(Mandatory)][string]$Worktree,   # path to remove
        [string]$Label = ''                        # e.g. "#1234" for log lines
    )

    if (-not (Test-Path -LiteralPath $Worktree)) {
        git -C $Repo worktree prune
        return
    }

    # Preserve genuine uncommitted work (tracked changes only — untracked is artifacts).
    $tracked = git -C $Worktree status --porcelain --untracked-files=no 2>$null
    if ($tracked) {
        Write-Host "Preserving dirty worktree $Label : $Worktree (uncommitted tracked changes)"
        return
    }

    $pending = Register-PendingWorktreeRemoval -Repo $Repo -Worktree $Worktree
    if (Remove-WorktreeDirectory -Repo $Repo -Worktree $Worktree -Label $Label) {
        if ($pending) { Remove-Item -LiteralPath $pending -Force -ErrorAction SilentlyContinue }
        Write-Host "Removed worktree $Label : $Worktree"
    } elseif ($pending) {
        Write-Host "WARNING: could not fully remove $Worktree; Remove-MergedWorktrees.ps1 retries the pending removal"
    } else {
        Write-Host "WARNING: could not fully remove $Worktree"
    }
}

# Git for Windows follows a directory junction while deleting a worktree: it empties the
# target, then fails with "Directory not empty" on the link and leaves a remnant without
# its .git file. Package managers link node_modules packages with junctions, and a junction
# can also point into another checkout or a shared store. Delete each link itself, never its
# target, before any recursive delete. Returns whether no directory link remains.
function Remove-WorktreeDirectoryLinks {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Worktree,
        [string]$Label = ''
    )

    if (-not $IsWindows) { return $true }

    # Get-ChildItem does not recurse through links, so every match is a link to remove.
    $find = { @(Get-ChildItem -LiteralPath $Worktree -Directory -Recurse -Force -Attributes ReparsePoint -ErrorAction SilentlyContinue) }
    foreach ($link in (& $find)) {
        # DirectoryInfo.Delete() without recursion removes the reparse point only.
        try { $link.Delete() } catch { Write-Host "Could not detach directory link $($link.FullName): $_" }
    }

    $remaining = & $find
    if ($remaining.Count -gt 0) {
        Write-Host "WARNING: worktree $Label kept: $($remaining.Count) directory link(s) could not be detached, first $($remaining[0].FullName): $Worktree"
        return $false
    }
    return $true
}

# Deletes an already-vetted worktree directory and prunes its registration. Returns
# whether the directory is gone. Callers must have checked that it holds no work.
function Remove-WorktreeDirectory {
    param(
        [Parameter(Mandatory)][string]$Repo,
        [Parameter(Mandatory)][string]$Worktree,
        [string]$Label = ''
    )

    if (-not (Remove-WorktreeDirectoryLinks -Worktree $Worktree -Label $Label)) { return $false }

    # Primary path: let git remove it (force clears untracked artifacts; tracked is clean).
    git -C $Repo -c core.longpaths=true worktree remove --force $Worktree 2>$null | Out-Null

    # Fallback for long-path failures and remnants that Git no longer recognizes.
    if (Test-Path -LiteralPath $Worktree) {
        if (-not (Remove-WorktreeDirectoryLinks -Worktree $Worktree -Label $Label)) { return $false }
        # \\?\ disables Win32 path normalization, so forward slashes (git's output
        # format) are NOT translated — convert to backslashes or the delete no-ops.
        $removalPath = if ($IsWindows) { '\\?\' + ($Worktree -replace '/', '\') } else { $Worktree }
        Remove-Item -LiteralPath $removalPath -Recurse -Force -ErrorAction SilentlyContinue
    }

    git -C $Repo worktree prune 2>$null | Out-Null
    return -not (Test-Path -LiteralPath $Worktree)
}

# AGENT EVIDENCE
# Agents keep logs, reports and PR-body drafts for a work item under
# <main checkout>-evidence/<lock name>/, beside the worktree root, so nothing lands
# loose in the parent directory. Evidence matters only while its item is active.
function Get-AgentEvidenceRoot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$MainCheckout)

    return "$(($MainCheckout -replace '\\', '/').TrimEnd('/'))-evidence"
}

# Removes each top-level entry of the evidence root whose newest write is older than
# the retention window. Returns the number of entries removed (or that would be).
function Remove-StaleAgentEvidence {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][int]$RetentionDays,
        [switch]$WhatIf
    )

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) { return 0 }
    $cutoff = [DateTime]::UtcNow.AddDays(-$RetentionDays)
    $removed = 0
    foreach ($entry in (Get-ChildItem -LiteralPath $Root -Force -ErrorAction SilentlyContinue)) {
        if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            Write-Host "sweep: skipping evidence entry that is a link: $($entry.FullName)"
            continue
        }
        $items = @($entry)
        if ($entry.PSIsContainer) {
            $items += @(Get-ChildItem -LiteralPath $entry.FullName -Recurse -Force -ErrorAction SilentlyContinue)
        }
        $newest = ($items | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum
        if ($newest -ge $cutoff) { continue }
        if ($WhatIf) {
            Write-Host "sweep: WOULD remove stale evidence $($entry.FullName)"
            $removed++
            continue
        }
        if ($entry.PSIsContainer -and -not (Remove-WorktreeDirectoryLinks -Worktree $entry.FullName -Label '(evidence)')) { continue }
        Remove-Item -LiteralPath $entry.FullName -Recurse -Force -ErrorAction SilentlyContinue
        if (Test-Path -LiteralPath $entry.FullName) {
            Write-Host "sweep: WARNING could not remove stale evidence $($entry.FullName)"
            continue
        }
        Write-Host "sweep: removed stale evidence $($entry.FullName)"
        $removed++
    }
    return $removed
}

# PENDING REMOVALS
# On Windows a directory cannot be deleted while any process has its current directory
# inside it, for example a shell that ran Merge-Pr.ps1 from the worktree, and build or
# test processes can hold files open briefly. Git then deletes part of the tree,
# including the .git file, and fails. The remnant no longer looks like a worktree, so
# the orphan sweep never recognizes it. Before deleting a vetted checkout, cleanup
# records it under the common Git directory. The record is removed after success, and
# Complete-PendingWorktreeRemovals retries the rest once the blocking process has exited.

function Get-PendingWorktreeRemovalDirectory {
    param([Parameter(Mandatory)][string]$Repo)

    $common = git -C $Repo rev-parse --path-format=absolute --git-common-dir 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $common) { return $null }
    return Join-Path $common.Trim() 'agent-pending-removals'
}

function Register-PendingWorktreeRemoval {
    param(
        [Parameter(Mandatory)][string]$Repo,
        [Parameter(Mandatory)][string]$Worktree
    )

    $directory = Get-PendingWorktreeRemovalDirectory -Repo $Repo
    if (-not $directory) { return $null }
    $path = [IO.Path]::GetFullPath($Worktree).TrimEnd([char[]]@('/', '\'))
    $gitDirectory = git -C $path rev-parse --absolute-git-dir 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $gitDirectory) { return $null }

    $key = if ($IsWindows) { $path.ToLowerInvariant() } else { $path }
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($key)))
    $record = Join-Path $directory ($hash.Substring(0, 16).ToLowerInvariant() + '.json')
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    [ordered]@{
        path         = $path
        gitDirectory = [IO.Path]::GetFullPath($gitDirectory.Trim())
        recordedAt   = [DateTimeOffset]::UtcNow.ToString('o')
    } | ConvertTo-Json -Compress | Set-Content -LiteralPath $record -NoNewline
    return $record
}

function Test-SameNativePath {
    param(
        [Parameter(Mandatory)][string]$Left,
        [Parameter(Mandatory)][string]$Right
    )

    try {
        $leftPath = [IO.Path]::GetFullPath($Left).TrimEnd([char[]]@('/', '\'))
        $rightPath = [IO.Path]::GetFullPath($Right).TrimEnd([char[]]@('/', '\'))
        $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
        return [string]::Equals($leftPath, $rightPath, $comparison)
    }
    catch {
        return $false
    }
}

function Test-HasWorkNewerThan {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][DateTimeOffset]$Cutoff
    )

    # Build artifacts are not work; everything else written after the cutoff is.
    $ignoredSegments = '(^|[\\/])(?:bin|obj|node_modules|TestResults)[\\/]'
    foreach ($file in (Get-ChildItem -LiteralPath $Path -File -Recurse -Force -ErrorAction SilentlyContinue)) {
        $relative = [IO.Path]::GetRelativePath($Path, $file.FullName)
        if ($relative -eq '.git' -or $relative -match $ignoredSegments) { continue }
        if ($file.LastWriteTimeUtc -gt $Cutoff.UtcDateTime) { return $true }
    }
    return $false
}

function Complete-PendingWorktreeRemovals {
    param(
        [Parameter(Mandatory)][string]$Repo,
        [switch]$WhatIf
    )

    $directory = Get-PendingWorktreeRemovalDirectory -Repo $Repo
    if (-not $directory -or -not (Test-Path -LiteralPath $directory)) { return 0 }
    $completed = 0
    foreach ($recordFile in (Get-ChildItem -LiteralPath $directory -File -Filter '*.json')) {
        try {
            $record = Get-Content -LiteralPath $recordFile.FullName -Raw | ConvertFrom-Json
            $path = [string]$record.path
            $recordedAt = [DateTimeOffset]::Parse([string]$record.recordedAt, [Globalization.CultureInfo]::InvariantCulture)
        }
        catch {
            Write-Host "sweep: WARNING unreadable pending removal record: $($recordFile.FullName)"
            continue
        }
        $drop = { if (-not $WhatIf) { Remove-Item -LiteralPath $recordFile.FullName -Force -ErrorAction SilentlyContinue } }

        if (-not $path -or -not (Test-Path -LiteralPath $path)) { & $drop; continue }

        # A new checkout at the same path has its own Git metadata, created after the record.
        $marker = Join-Path $path '.git'
        if (Test-Path -LiteralPath $marker -PathType Leaf) {
            $gitDirectory = ((Get-Content -LiteralPath $marker -TotalCount 1) -replace '^gitdir:\s*', '').Trim()
            if ($gitDirectory -and -not [IO.Path]::IsPathRooted($gitDirectory)) { $gitDirectory = Join-Path $path $gitDirectory }
            if ($gitDirectory -and (Test-Path -LiteralPath $gitDirectory) -and (
                    -not (Test-SameNativePath -Left $gitDirectory -Right ([string]$record.gitDirectory)) -or
                    (Get-Item -LiteralPath $gitDirectory -Force).CreationTimeUtc -gt $recordedAt.UtcDateTime)) {
                Write-Host "sweep: dropping pending removal superseded by a new checkout: $path"
                & $drop
                continue
            }
        }
        elseif (Test-Path -LiteralPath $marker) {
            Write-Host "sweep: dropping pending removal for a standalone repository: $path"
            & $drop
            continue
        }

        # The checkout was vetted before deletion began. Anything written since is new work.
        if (Test-HasWorkNewerThan -Path $path -Cutoff $recordedAt) {
            Write-Host "sweep: WARNING preserving pending removal with files changed after removal began; inspect manually: $path"
            & $drop
            continue
        }

        if ($WhatIf) {
            Write-Host "sweep: WOULD finish pending removal of $path"
            continue
        }
        if (Remove-WorktreeDirectory -Repo $Repo -Worktree $path -Label '(pending removal)') {
            & $drop
            $completed++
            Write-Host "sweep: finished pending removal of $path"
        } else {
            Write-Host "sweep: WARNING pending removal is still blocked, retrying next sweep: $path"
        }
    }
    return $completed
}
