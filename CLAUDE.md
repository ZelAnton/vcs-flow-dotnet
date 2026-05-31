# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

**Vcs.Flow** is a set of *combined* version-control workflow commands for git and
jujutsu (`jj`). Each command wraps a multi-step VCS operation behind an interactive
console UI — e.g. `commit` lists changed files, lets you pick what to stage, collects
a message, and offers to push afterwards. Each command is a separate project that
compiles to a **standalone Native-AOT executable** named after the verb; shared logic
lives in one `Vcs.Flow.Common` library.

[AGENTS.md](AGENTS.md) is the authoritative, detailed convention set. This file is the
fast-start map; where the two overlap, AGENTS.md wins.

## Commands

```bash
# Build everything (warnings are errors; build order from the .slnx resolves each
# command's assembly Reference to the freshly built Vcs.Flow.Common).
dotnet build Vcs.Flow.slnx

# Run all tests (build first, as above)
dotnet test Vcs.Flow.slnx

# Run a single test
dotnet test Vcs.Flow.slnx --filter "FullyQualifiedName~ParsePorcelain"

# Produce a native binary for one command (needs the platform C/C++ toolchain:
# clang+zlib1g-dev on Linux, MSVC build tools on Windows, Xcode CLT on macOS).
dotnet publish src/Vcs.Flow.Commit/Vcs.Flow.Commit.csproj -c Release
```

## Architecture

```
Vcs.Flow.slnx                      solution (.slnx; explicit BuildDependency ordering)
Directory.Build.props              repo-wide MSBuild + $(RepoRoot), $(CommonProjectDir)
Directory.Packages.props           central package versions
src/Directory.Build.props          shared app/AOT settings (imports the root props)
src/Vcs.Flow.Common/               shared library  (IsAotCompatible)
  VcsRunner.cs                     ProcessKit-backed git/jj/gh invocation + capture
  WorkingTree.cs                   pure parsers for VCS porcelain output (unit-testable)
src/Vcs.Flow.Commit/               -> binary `commit`  (OutputType=Exe, PublishAot)
src/Vcs.Flow.Push/                 -> binary `push`
tests/Vcs.Flow.Common.Tests/       NUnit tests for the shared library
```

The layering is the key idea:

- **`Vcs.Flow.Common`** holds everything reused across commands — the VCS/process
  layer and pure helpers. It does **not** reference Spectre.Console; keep interactive
  UI out of it.
- **Each command project** (`Vcs.Flow.<Verb>`) is a top-level-statement `Program.cs`
  that drives the Spectre.Console UI and calls into `Vcs.Flow.Common`. Its
  `AssemblyName` is the bare verb (`commit`, `push`), so the binary is `commit` /
  `commit.exe`.
- **Process execution always flows through `VcsRunner` → ProcessKit's
  `IProcessRunner`** — never `Process.Start` directly, and never parse human-readable
  CLI output (use `--json` / `jj` templates). `VcsRunner` takes an optional
  `IProcessRunner` so command logic can be tested against a fake instead of spawning a
  real process.

### Adding a new command

1. `src/Vcs.Flow.<Verb>/Vcs.Flow.<Verb>.csproj` — copy an existing command's csproj:
   `OutputType=Exe`, `AssemblyName=<verb>`, `PublishAot=true`, a
   `<Reference Include="Vcs.Flow.Common" />` with `AssemblySearchPaths` set to
   `$(CommonProjectDir)bin\...`, and `PackageReference`s for `ProcessKit` +
   `Spectre.Console`.
2. Add the project to `Vcs.Flow.slnx` with a `BuildDependency` on
   `Vcs.Flow.Common`.
3. Put shared logic in `Vcs.Flow.Common` (+ a test); keep only the command's own
   orchestration/UI in its `Program.cs`.
4. Add a `CHANGELOG.md` bullet and a README table row.

### Why `Reference` instead of `ProjectReference` (load-bearing)

Cross-project references use `<Reference>` + `AssemblySearchPaths`, never
`ProjectReference` — a hard convention shared with the umbrella's other .NET projects.
Consequence to remember: the assembly `Reference` brings only `Vcs.Flow.Common.dll`,
**not** its NuGet dependencies. That's why each command also `PackageReference`s
`ProcessKit` (and `Spectre.Console`) directly — without it the native publish can't
resolve those assemblies. Build order is therefore not implicit; it comes from the
`BuildDependency` entries in `Vcs.Flow.slnx`, so build (or let the test runner build)
before tests/commands can resolve the reference.

## Native AOT is a hard constraint

`src/Directory.Build.props` sets `IsAotCompatible`/`PublishAot` for everything under
`src/`, which turns the trim/AOT/single-file analyzers on. Combined with
`TreatWarningsAsErrors`, AOT-unsafe code (reflection, `dynamic`, reflection-based
serialization) is a **build error**, not a publish-time surprise. CI's `aot-publish`
job links each command natively on Linux and Windows. A local native publish that fails
only at the final link step with a `vswhere.exe`/MSVC error is an environment/PATH
issue (missing C++ toolchain), not a code defect — the managed→native codegen having
run is the signal the project itself is AOT-correct.

## Pending dependencies (important context)

This toolkit is meant to drive git/jj/gh through the umbrella's typed CLI wrappers, but
**`Vcs.Git` / `Vcs.Jujutsu` / `Vcs.GitHub` (and the older `GhRun` / `JjRun`) are not
yet published to NuGet.org.** Until they are, commands shell out through
`VcsRunner`/ProcessKit directly. When they publish, add them to
`Directory.Packages.props` and migrate the raw shell-outs onto the typed wrappers.
Also note: the `ConsoleKit` id on nuget.org belongs to an unrelated author — do not add
it; use Spectre.Console directly. See [AGENTS.md](AGENTS.md#pending-dependencies).

## Conventions quick reference

Tabs for indentation (C#/MSBuild/config), spaces in YAML/PowerShell, LF endings,
file-scoped namespaces — all from `.editorconfig`. Central Package Management (no
inline versions). No one-line `try`/`catch`/`finally`; every empty `catch` carries a
rationale comment. Every user-visible change ships a `CHANGELOG.md` bullet under
`## [Unreleased]` in the same change set. The full rules — exception style, comments,
documentation, command conventions — are in [AGENTS.md](AGENTS.md).

## Version control workflow

Colocated git + `jj`; drive everything through `jj`, not raw `git`. Describe work
early (`jj describe -m "..."`), fold small follow-ups into the current change, and
**never push without an explicit `pull`/`push`/`sync` signal**. Work lands on `main`;
do not create bookmarks unless asked. Undo via `jj undo` / `jj abandon` / `jj restore`.
Full handshake and rationale: [AGENTS.md](AGENTS.md#version-control-jujutsu).

## CI & security

- `.github/workflows/ci.yml` — YAML lint, cross-platform build+test (Linux/Windows/
  macOS), and a Native-AOT publish check per command on Linux/Windows.
- `.github/workflows/codeql.yml` — CodeQL `security-and-quality`, `build-mode: manual`.
- Dependencies audited on restore (`NuGetAudit`); Dependabot bumps actions + packages
  weekly. There is **no NuGet release pipeline** — these are applications, not packages.

## Agent tooling notes (Windows host)

Two harness pitfalls that have already cost a wasted batch here — read before
running shell commands:

- **Invoke Python as `py`, never bare `python`.** The interpreter
  (`C:\Python3xx\python.exe`) is not on PATH in *either* tool, so `python ...` fails
  with exit 127 / "not recognized" in both the Bash and PowerShell tools. The Windows
  launcher `py` (`C:\Windows\py.exe`) *is* on PATH in both — use it (or the full
  interpreter path). Separately, PowerShell cmdlets (`Get-ChildItem`, …) work only in
  the PowerShell tool; POSIX utilities (`grep`, `sed`, `find`) in the Bash tool.
  Probe if unsure: `command -v py` (Bash) / `Get-Command py` (PowerShell).
- **A single failing call cancels its whole parallel batch.** If any tool call in a
  batch errors — a non-zero exit, a missing command/module, an `Edit` whose
  `old_string` is not found, a denied permission — the harness cancels every sibling
  call (`Cancelled: parallel tool call ... errored`). Run fail-prone calls (probes,
  validation, uncertain edits) **in their own message as a lone call** — "on their
  own" means *no siblings in the batch*, not just a separate call in the same batch.
  Only batch calls that are mutually independent **and** all expected to succeed.
- **No local YAML/lint validation here.** `yamllint` is not on PATH and `py` has no
  `yaml` module, so you cannot validate workflow YAML locally — don't waste calls
  trying. Rely on CI's `yaml-lint` job (`.github/workflows/ci.yml`).
