# Local regression suite for WorktreeCleanup.ps1: requires Git and PowerShell 7.
# A process whose current directory is inside a worktree blocks its deletion on
# Windows. Cleanup must record the interrupted removal and finish it later.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'WorktreeCleanup.ps1')
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "worktree-cleanup-tests-$([Guid]::NewGuid().ToString('N'))"
$repo = Join-Path $testRoot 'repo'
$records = Join-Path $repo '.git/agent-pending-removals'

function Invoke-TestGit {
    & git @args 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Git failed: $args" }
}
function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

if (-not $IsWindows) {
    Write-Output 'SKIP worktree cleanup: busy directories only block deletion on Windows.'
    return
}

try {
    New-Item -ItemType Directory -Path $repo -Force | Out-Null
    Invoke-TestGit init --initial-branch=main $repo
    Invoke-TestGit -C $repo config user.email 'cleanup-tests@example.invalid'
    Invoke-TestGit -C $repo config user.name 'Cleanup Tests'
    Set-Content (Join-Path $repo '.gitignore') 'bin/'
    Set-Content (Join-Path $repo 'source.txt') 'committed'
    Invoke-TestGit -C $repo add .
    Invoke-TestGit -C $repo commit -m fixture

    foreach ($kind in @('busy', 'busy-new-work', 'busy-reused')) {
        $path = Join-Path $testRoot $kind
        Invoke-TestGit -C $repo worktree add -b $kind $path HEAD
        New-Item -ItemType Directory (Join-Path $path 'bin') | Out-Null
        Set-Content (Join-Path $path 'bin/generated.dll') 'generated'
        $holder = Start-Process pwsh -ArgumentList '-NoProfile', '-Command', 'Start-Sleep 300' -WorkingDirectory $path -WindowStyle Hidden -PassThru
        try {
            Start-Sleep -Seconds 2
            Remove-MergedWorktree -Repo $repo -Worktree $path -Label "#$kind"
            Assert (Test-Path $path) "$kind checkout was removed while a process used it; the test cannot reproduce the lock."
            Assert (@(Get-ChildItem $records -Filter '*.json').Count -eq 1) "$kind removal did not record a pending removal."
            if ($kind -eq 'busy-new-work') { Set-Content (Join-Path $path 'notes.md') 'written after removal began' }
        }
        finally { Stop-Process -Id $holder.Id -Force -ErrorAction SilentlyContinue; $holder.WaitForExit() }
        if ($kind -eq 'busy-reused') {
            Remove-Item -LiteralPath $path -Recurse -Force
            Invoke-TestGit -C $repo worktree prune
            Start-Sleep -Seconds 1
            Invoke-TestGit -C $repo worktree add -b "$kind-again" $path HEAD
        }

        $null = Complete-PendingWorktreeRemovals -Repo $repo
        Assert (@(Get-ChildItem $records -Filter '*.json').Count -eq 0) "$kind pending removal record survived the sweep."
        switch ($kind) {
            'busy' {
                Assert (-not (Test-Path $path)) 'Pending removal was not completed after the process exited.'
                Assert ((& git -C $repo worktree list --porcelain) -notcontains "worktree $($path -replace '\\', '/')") 'Pending removal left a registration.'
            }
            'busy-new-work' { Assert (Test-Path (Join-Path $path 'notes.md')) 'Pending removal deleted work written after removal began.' }
            'busy-reused' { Assert (Test-Path (Join-Path $path 'source.txt')) 'Pending removal deleted a new checkout at the same path.' }
        }
    }

    # Git follows a directory junction while deleting a worktree and empties its target.
    $outside = Join-Path $testRoot 'outside'
    New-Item -ItemType Directory $outside | Out-Null
    Set-Content (Join-Path $outside 'keep.txt') 'outside the worktree'
    $linked = Join-Path $testRoot 'linked'
    Invoke-TestGit -C $repo worktree add -b linked $linked HEAD
    New-Item -ItemType Directory (Join-Path $linked 'bin') | Out-Null
    New-Item -ItemType Junction -Path (Join-Path $linked 'bin/pkg') -Target $outside | Out-Null
    Remove-MergedWorktree -Repo $repo -Worktree $linked -Label '#linked'
    Assert (Test-Path (Join-Path $outside 'keep.txt')) 'Worktree removal deleted files through a junction outside the worktree.'
    Assert (-not (Test-Path $linked)) 'Worktree with a directory junction was not removed.'

    Write-Output 'OK worktree cleanup: busy-directory pending removal, new-work and reused-path preservation, junction targets kept.'
}
finally {
    $resolved = [IO.Path]::GetFullPath($testRoot)
    $temp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($temp, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force -ErrorAction SilentlyContinue
}
