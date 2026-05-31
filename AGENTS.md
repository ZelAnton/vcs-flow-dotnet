# AGENTS.md

## Project

- This repository is **Vcs.Flow**: a set of combined version-control workflow commands for git and jujutsu (`jj`).
- Each command is its own project under `src/` that compiles to a **Native-AOT executable** named after the verb (`commit`, `push`, …).
- Shared, reusable logic lives in `src/Vcs.Flow.Common`; tests live under `tests/`.
- Keep each command focused on orchestrating one combined VCS operation behind an interactive console UI. Put anything reused by more than one command in `Vcs.Flow.Common`, not in a command project.

## Runtime

- Use .NET (target framework `net10.0`, set once in the root `Directory.Build.props`).
- Do not change the target framework unless explicitly asked.
- Use the repository-wide language settings from `Directory.Build.props`.

## Dependencies

- Do not introduce new NuGet packages without explicit approval.
- Use centralized package management. Manage versions only in `Directory.Packages.props`; do not put `Version` on individual `PackageReference` items.
- Process execution goes through **ProcessKit**'s `IProcessRunner` (`Vcs.Flow.Common.VcsRunner`) — never call `System.Diagnostics.Process.Start` directly.
- Never parse human-readable CLI output. Pass `--json` / `jj` templates and deserialize structured output.
- Interactive console UI uses **Spectre.Console**, and lives in the command projects (not in `Vcs.Flow.Common`).

### Pending dependencies

- The typed VCS CLI wrappers this toolkit is meant to build on — `Vcs.Git` / `Vcs.Jujutsu` / `Vcs.GitHub` (and the older `GhRun` / `JjRun`) — are **not yet published to NuGet.org**. Until they are, commands shell out through `VcsRunner`/ProcessKit directly. When they publish, add them to `Directory.Packages.props` and migrate the raw shell-outs onto the typed wrappers.
- The package id `ConsoleKit` on nuget.org is an unrelated author's package — do **not** add it expecting the umbrella's ConsoleKit. Use Spectre.Console directly until that library is published under a known id.

## Native AOT

- Every command project sets `<PublishAot>true</PublishAot>`; the shared library sets `<IsAotCompatible>true</IsAotCompatible>` (both via `src/Directory.Build.props`). This turns the trim/AOT/single-file analyzers on for everything under `src/`.
- With `TreatWarningsAsErrors`, any reflection-based or otherwise AOT-unsafe pattern is a **build error** — no `dynamic`, no reflection-heavy serialization, no `Newtonsoft` without source generation.
- The native link step needs the platform C/C++ toolchain (clang + `zlib1g-dev` on Linux, MSVC build tools on Windows). CI's `aot-publish` job verifies each command links on Linux and Windows.

## Project References

- Do not use `ProjectReference`. Cross-project references must use `Reference`; do not use `HintPath`.
- A project that references another project's output defines `AssemblySearchPaths` pointing at that project's `bin\$(Configuration)\$(TargetFramework)` directory (use the `$(CommonProjectDir)` path property, not `..\..\`).
- Every command project and the test project reference `Vcs.Flow.Common` this way. Because the assembly `Reference` does **not** flow the referenced project's NuGet dependencies, a command that needs ProcessKit/Spectre at runtime also lists those as its own `PackageReference`.
- Build ordering is enforced by `BuildDependency` entries in `Vcs.Flow.slnx` (referencing projects depend on referenced projects).

## Repository Structure

- `Vcs.Flow.slnx` is the solution file (`.slnx` format, explicit build dependencies).
- Root `Directory.Build.props` holds repository-wide MSBuild config and the path properties; `src/Directory.Build.props` holds the shared app/AOT settings and imports the root one.
- `Directory.Packages.props` holds centralized package versions.
- Source under `src/` (one folder per command + `Vcs.Flow.Common`), tests under `tests/`, helper scripts under `scripts/`.

## MSBuild Path Properties

- `Directory.Build.props` defines:
	- `$(RepoRoot)` — absolute path to the repository root (trailing separator), from `$(MSBuildThisFileDirectory)`.
	- `$(CommonProjectDir)` — absolute path to `src/Vcs.Flow.Common/`.
- Use these instead of relative constructs (`..\..\`, `$(MSBuildThisFileDirectory)..\`) whenever a project file references something outside its own directory.
- If a new project is added that other projects reference by path, add a corresponding `$(XxxProjectDir)` property.

## Build And Test

- `dotnet build Vcs.Flow.slnx` validates compilation (warnings are errors).
- `dotnet test Vcs.Flow.slnx` runs the suite; a real run reports NUnit discovery and a test summary (`Passed! - Failed: 0, ...`), not only completed MSBuild targets.
- Because project-to-project references use `Reference`, build ordering comes from `Vcs.Flow.slnx`.

## Formatting

- `.editorconfig` is the source of truth. Tabs for indentation in C#, MSBuild, and config files (`.cs`, `.csproj`, `.props`, `.targets`, `.slnx`, `.json`, `.config`, `.md`); spaces in YAML (`.yml`) and PowerShell (`.ps1`).
- Do not mix tabs and spaces within a file. Preserve LF line endings (except `.cmd`/`.bat`, which require CRLF).

## C# Style

- File-scoped namespaces; nullable and implicit usings enabled; warnings as errors.
- Prefer simple, direct code over new abstractions. Keep `Vcs.Flow.Common`'s public surface small and intentional; keep implementation details internal.

### Exception handling style

- **No one-line `try`/`catch`/`finally`.** Every `try`, `catch`, and `finally` owns a brace block on its own lines. `try { foo(); } catch { }` collapsed onto one line is a style violation.
- **Empty `catch` blocks must carry a comment** explaining what is swallowed and why doing nothing is correct — both the expected exception and the rationale. `// ignored` alone is not enough.
	```csharp
	try
	{
		_cts.Cancel();
	}
	catch (ObjectDisposedException)
	{
		// already disposed - being torn down concurrently; nothing to recover.
	}
	```

## Comments

- Minimize comments. Write them only to explain *why* something exists, an architectural decision, or non-obvious platform/runtime behaviour — not to restate what the code already says.

## Documentation

- All documentation and code comments are in English.
- When a command's behaviour, flags, or the public API of `Vcs.Flow.Common` changes, update the README and the relevant docs in the same change set.

## Changelog

- `CHANGELOG.md` is the single source of truth for notable changes.
- **Every user-visible change ships a `CHANGELOG.md` entry in the same change set**, under `## [Unreleased]` (`### Added/Changed/Fixed/Removed/Deprecated`). One bullet per distinct effect, written for a user of the commands. Pure internal refactors with no observable effect are the only exemption.
- There is no automated release pipeline yet (these are applications, not NuGet packages), so keep the changelog accurate by hand.

## Security Scanning

- `.github/workflows/codeql.yml` runs CodeQL (`security-and-quality`, `build-mode: manual` driving `dotnet build Vcs.Flow.slnx`) on PRs, pushes to `main`, and weekly. Treat new alerts like build warnings; dismiss confirmed false positives in the GitHub UI with a written justification rather than narrowing the scan.

## Version control (jujutsu)

This repository is colocated git + `jj`; `jj` is the primary tool — use `jj`, not raw `git`.

### Describing the current change

- Set the change description when you start work: `jj describe -m "Concise summary"`. Fold small follow-ups into the current change rather than spawning one per edit; re-`describe` if the scope shifts.

### Starting unrelated work

- Current change finished → `jj new -m "..."` (descendant). Still in progress → `jj new @- -m "..."` (parallel sibling). Do not silently mix unrelated work into one change.

### Pushing to remote

The user signals sync with a short trigger (`pull`/`push`/`sync`). On that signal:
1. `jj git fetch` first.
2. Rebase if `main@origin` advanced: `jj rebase -r @- -d main@origin`.
3. `jj bookmark set main -r <rev>` then `jj git push --bookmark main`.

Never push without an explicit signal.

### Undoing work

- `jj undo` reverses the last operation (repeatable); `jj abandon <rev>` drops a change; `jj restore` discards working-copy edits; `jj op log` + `jj op restore <op-id>` reach any prior point. Tell the user what was reverted.

### Bookmarks

- Work happens on `main`. Do not create new bookmarks unless the user explicitly asks.

## Command Conventions

- Commands should be idempotent where possible.
- Output should remain concise and script-friendly.
- Breaking changes (renamed/removed commands or flags) must be explicit.
