# EnumerableAsyncProcessor

Read [AGENTS.md](../../../../AGENTS.md) for build/test commands and lifecycle contracts.

- Validate all supported target frameworks with the full suite, including the oldest framework's fallback paths.
- Processor, throttling, and scheduling changes need cancellation, exception-propagation, and completion-ordering coverage. Library/pipeline awaits use `ConfigureAwait(false)`; tests need not.
- No Aspire AppHost or external test services. Docker serves only shared lock Redis.

The mechanics this loop defers to:

| Need | Owner |
|---|---|
| One PR's review cycle: fetch every surface, verify findings, fix, one push, reply, resolve and unresolve threads | `AGENTS.md` § Pull request reviews and [Review cycle](#review-cycle) below |
| Merge protocol | [Merge gate](#merge-gate) below |
| Out-of-scope filing bar and procedure | `AGENTS.md` § Autonomous self-improvement; write every issue, discovery and triage ones included, as `AGENTS.md` [§ Writing issues](../../../../AGENTS.md#writing-issues) describes |
| Native sub-issue and `blocked_by` writes | [Issue relationships](#issue-relationships) below |
| Worktree location, evidence directory, cleanup | [Worktrees](#worktrees) below |
| PR review lifecycle | [PR review lifecycle](#pr-review-lifecycle) below |
| Build, test, and PR rules | `AGENTS.md` § Commands and [Implement and ship](#implement-and-ship) below |

Lock owner variable: `EAP_AGENT_LOCK_OWNER_ID` (read by `scripts/AgentLocks.ps1` after `-OwnerId`, before `CODEX_THREAD_ID`).

## Differences from the shared skill files

The shared files describe more than this repository has. These lines win over them:

- **No draft lifecycle.** Wherever `SKILL.md` or `references/recovery.md` says draft PR or draft review lifecycle, including recovery and stranded-branch PRs, open the PR ready for review and follow [PR review lifecycle](#pr-review-lifecycle).
- **No `scripts/Enable-AutoMerge.ps1`.** Never arm auto-merge; see [Merge gate](#merge-gate).
- **No `scripts/WorktreeLifecycle.md`.** [Worktrees](#worktrees) covers preservation and crash behavior.
- **`release` keeps the checkout.** `scripts/AgentLocks.ps1 release` only frees the lock. `scripts/Merge-Pr.ps1` and `scripts/Remove-MergedWorktrees.ps1` remove worktrees.
- **No priority tiers.** There are no `priority:*` or `ci` labels, so every issue is in the `priority:medium` tier. `scripts/Start-IssuePrLoop.ps1` does not rank by tier: after parents whose sub-issues are all closed, it takes issues that block the most others, then those with the fewest comments.

## Owner-reserved decisions

No issue class is reserved for the owner, so `needs-human` follows the normal triage rules. Defer as `[human]` only for the reasons in `SKILL.md` § Resolve, do not defer. A breaking change is allowed on a milestone that permits breaking changes.

## PR review lifecycle

This repository has no draft review lifecycle. Open every PR ready for review with `gh pr create`, including recovery and split PRs; open a draft only when the user explicitly requests one. The loop never promotes a user's draft, and `scripts/Start-IssuePrLoop.ps1` skips drafts.

- `.github/workflows/claude-code-review.yml` (job `claude-review`) runs on `opened`, `synchronize`, `ready_for_review`, and `reopened`, drafts included. It posts its review as a `github-actions[bot]` conversation comment with no head marker; the `claude-review` check on the current head ties the review to that head. When a head has no review (a failed or cancelled run), dispatch it once: `gh workflow run claude-code-review.yml -f pr_number=<N>`.
- `claude-review` skips `dependabot[bot]` PRs and `renovate/*` branches, so they get no review cycle.
- A new PR needs no reviewer request, and no approval is required. Findings from any other review app are review surfaces like any other.

Automated review rounds (rework budget): `gh api --paginate "repos/{owner}/{repo}/issues/<N>/comments" --jq '.[] | select(.user.login == "github-actions[bot]") | .id' | wc -l`.

Draft CI: `.github/workflows/dotnet.yml` and `.github/workflows/aot.yml` have no draft filter, so CI runs on drafts.

## Review cycle

Run this under the PR lock. Inspect every surface, including pagination and replies: REST `pulls/<N>/reviews`, `pulls/<N>/comments`, and `issues/<N>/comments` under `repos/{owner}/{repo}`, and GraphQL `reviewThreads` with IDs, resolution state, bodies, authors, and timestamps.

Never infer a reviewer's surface from its identity. Treat a conversation comment that carries findings or a verdict as a review; it has no thread to resolve, so disposition it with one PR comment that quotes or links it.

A `COMMENTED` review body can block with zero unresolved threads. Address outstanding concerns, `CHANGES_REQUESTED`, current inline findings, and unresolved threads. Empty acknowledgments are non-blocking. Assess older findings against the current head and later discussion; uncertain concerns block merging.

Verify every finding against the code and classify it:

- **Blocking:** a correctness, concurrency, security, contract, or performance defect, or missing tests for changed behavior. Fix each valid blocking finding in this PR.
- **Non-blocking:** naming, style, wording, optional refactors, extra tests for unchanged behavior, and "consider" suggestions. Implement one only when it is small and you already push a blocking fix. Otherwise decline it in the disposition with a one-line reason and push nothing for it.
- Decline a wrong finding with evidence from the code. Move architectural work that is genuinely out of scope to a filed follow-up issue, linked in the reply.

"Not done" or "left as is" without a classification, reason, or linked issue is not a disposition. Reply once per review covering each finding, and reply on each thread. Discuss disagreement once; implement reaffirmed blocking feedback. A disposition-only reply on the current head needs no new push, so it does not start another review cycle.

Resolve a thread with GraphQL `resolveReviewThread` as soon as it is dispositioned, whether a human or a bot opened it: the fix commit is in the remote PR head and the reply names it, or the reply pushes back with evidence. Do not wait for another review or CI cycle to resolve it; that cycle still gates merging. Leave a thread open only while it has no disposition: the fix is not pushed, the item waits on the user, or the reviewer replied after your disposition. On each later cycle re-fetch every thread, resolved ones included; a reviewer reply newer than your disposition reopens it: run GraphQL `unresolveReviewThread` and handle it as reaffirmed feedback.

Reply to addressed review-body findings and request re-review when an outstanding changes-requested review needs updating; do not dismiss reviews to bypass approval requirements. After resolving, re-fetch reviews, threads, checks, and head SHA. After a fix push, allow a subsequent bot review and CI cycle before considering merge in a later iteration.

## Merge gate

Merge requires all of these on the current head; reassess if the head changes:

- An open, ready-for-review PR that is `MERGEABLE` and `CLEAN`.
- Every applicable check present, terminal, and passing (`SUCCESS`, `SKIPPED`, or `NEUTRAL`, with successful legacy commit statuses). Expected checks: `modularpipeline`, `aot-test (net8.0)`, `aot-test (net10.0)`, and `claude-review`. A missing expected check or a nonterminal check blocks merging.
- No unresolved threads, unaddressed comments, or outstanding review-body concerns.
- Approval when required, and a bot review and CI cycle after the last fix push.

`main` branch protection requires only an up-to-date `modularpipeline`, with no required approvals or conversation resolution. It cannot read review bodies, so the scripts and you cover the rest. A PR behind `main` is not `CLEAN`: rebase it onto fresh `origin/main`. Read the current rules with `gh api "repos/{owner}/{repo}/branches/main/protection"` and `gh api "repos/{owner}/{repo}/rulesets"`.

- `scripts/Assert-PrGreen.ps1 -Pr <N>` is the read-only gate: open, `MERGEABLE`, `CLEAN`, every reported check passing, no changes requested, no actionable top-level review body, no unresolved threads. It does not read draft state or know the expected checks, so confirm those yourself.
- `scripts/Merge-Pr.ps1 -Pr <N> -Worktree <path>` runs that gate, squash-merges, removes the PR's worktree, then deletes the local and remote head branches. A worktree with uncommitted tracked changes is preserved, with both branches. Run it from the shared checkout.
- Wait for every check, then merge with `Merge-Pr.ps1`; take another item meanwhile. There is no auto-merge script and no CI tiers. Never bypass the wrapper with direct `gh pr merge` or `--auto`.
- Before merging, read every review body, inline thread, and conversation comment on the current head and disposition each finding per [Review cycle](#review-cycle). The scripts are necessary but not sufficient.

## Issue relationships

Relationship writes take the numeric REST issue `.id`, not the issue number or `gh issue view --json id` (a GraphQL ID). Pass it with `-F` so it is sent as an integer:

```powershell
$childId = gh api "repos/$repoSlug/issues/<child>" --jq .id
gh api "repos/$repoSlug/issues/<parent>/sub_issues" -X POST -F "sub_issue_id=$childId"

$blockerId = gh api "repos/$repoSlug/issues/<blocker>" --jq .id
gh api "repos/$repoSlug/issues/<blocked>/dependencies/blocked_by" -X POST -F "issue_id=$blockerId"
```

Verify each write with `gh api "repos/$repoSlug/issues/<parent>/sub_issues"` or `gh api "repos/$repoSlug/issues/<blocked>/dependencies/blocked_by"`.

## Worktrees

Capture these beside the variables in `SKILL.md` § Isolation and ownership:

```powershell
$repoName = ($repoSlug -split '/')[-1]
$worktreeRoot = Join-Path (Split-Path $repo -Parent) ($repoName + '-worktrees')
```

Verify the repository and intended base; examples use `origin/main`. Use a writable temporary `<repoName>-worktrees` root if needed. Choose a unique worktree path when one already exists. No sparse-checkout is needed.

```powershell
git fetch origin main
git worktree add (Join-Path $worktreeRoot 'issue-<N>-<short-desc>') -b issue-<N>-<short-desc> origin/main
git push -u origin issue-<N>-<short-desc>   # first push, from the worktree: the branch starts out tracking origin/main
```

After `gh pr checkout <N>`, a plain `git push` reaches the PR branch. Shell directory changes do not persist between tool calls. Compare `git rev-parse --show-toplevel` with the intended worktree's resolved absolute path. `renew -Worktree` records metadata; keep passing explicit lock names.

Write nothing loose beside the repository. Every file an agent produces outside a worktree has one home, so cleanup can find it:

- Evidence (build and test logs, reports, PR and comment body drafts, diffs, validation projects) goes in `<repo-parent>/<repoName>-evidence/<lock-name>/`, for example `C:/git/EnumerableAsyncProcessor-evidence/issue-123/build.log`. `scripts/Remove-MergedWorktrees.ps1` deletes an entry once its newest file is older than `-EvidenceRetentionDays` (default 7).
- Throwaway checkouts and baselines are worktrees under `$worktreeRoot`, never plain copies.
- Downloaded SDKs and CLI tools go in `<repo-parent>/toolchains/<name>-<version>/`, shared by every repository.
- Never write to `<repo-parent>` itself or to the main checkout.

Use the repository scripts for merged-worktree cleanup; `[gone]` or ancestry alone is insufficient. `scripts/Remove-MergedWorktrees.ps1` preserves dirty, locked, open-PR, harness-managed, and locally divergent worktrees and lists any with no merge evidence. Inspect untracked source before cleanup: scripts may clear build artifacts. Remove an abandoned worktree by hand only after verifying the repository and path, ownership, and that it holds no valuable unpublished work.

Crash behavior: a removal in progress is recorded under `<git-common-dir>/agent-pending-removals`, and every sweep retries what is left there. The sweep does not unlock any git lock, so there is no setup-lock exception here.

Stop only services, processes, and containers started for this worktree. Never blanket-delete containers or volumes, or stop shared lock Redis.

## Implement and ship

- Follow local validation, resource limits, target frameworks, generated-code boundaries, and performance requirements. Run focused and relevant broader checks, including docs checks when affected.
- Use TDD for bugs and behavior changes where feasible; explain exceptions.
- Review the full diff for correctness, reuse, and efficiency (`/simplify` when available, otherwise equivalent review). Preserve performance and contracts, and batch related fixes into one push.
- Stop owned services when no longer needed. Use commentary while active.
