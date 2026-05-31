# Contributing to Vcs.Flow

Thanks for your interest in improving **Vcs.Flow**.

## Prerequisites

- .NET 10 SDK (the exact band is pinned in [`global.json`](global.json)).
- For the native publish: the platform C/C++ toolchain Native-AOT links against
  (clang + `zlib1g-dev` on Linux, MSVC build tools on Windows, Xcode CLT on macOS).

## Build and test

```sh
dotnet build Vcs.Flow.slnx
dotnet test  Vcs.Flow.slnx
```

The build treats **warnings as errors** and enforces code style on build, so a clean
local build is required before opening a pull request. Run a single test with:

```sh
dotnet test Vcs.Flow.slnx --filter "FullyQualifiedName~ParsePorcelain"
```

Verify a command still links under Native-AOT (CI does this for every command on
Linux and Windows):

```sh
dotnet publish src/Vcs.Flow.Commit/Vcs.Flow.Commit.csproj -c Release
```

## Conventions

- **Formatting** is governed by [`.editorconfig`](.editorconfig) — tabs for
  indentation, LF line endings, file-scoped namespaces. Do not reformat code you are
  not changing.
- **Dependencies** use Central Package Management — declare versions only in
  [`Directory.Packages.props`](Directory.Packages.props); `PackageReference` items
  carry no `Version`.
- **Cross-project references** use `Reference` + `AssemblySearchPaths`, never
  `ProjectReference`. Build order comes from `BuildDependency` in `Vcs.Flow.slnx`.
- **AOT safety** — code under `src/` must stay trim/AOT-clean (no reflection-based
  patterns); the analyzers run on every build.
- See [`AGENTS.md`](AGENTS.md) for the full, authoritative set of conventions.

## Changelog

Every user-visible change ships its [`CHANGELOG.md`](CHANGELOG.md) entry in the same
change set, under `## [Unreleased]`. Pure internal refactors are exempt.

## Pull requests

- Keep changes focused; unrelated cleanups belong in their own PR.
- Ensure CI (build/test on Linux, Windows, macOS; AOT publish; CodeQL) passes.
- Fill in the pull-request checklist.
