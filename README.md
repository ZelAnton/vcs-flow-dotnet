# Vcs.Flow

Combined version-control workflow commands for **git** and **jujutsu (`jj`)**. Each
command bundles a multi-step VCS operation behind an interactive console UI — for
example `commit` shows the changed files, lets you pick what to stage, collects a
message, and offers to push afterwards.

Every command is its own project that compiles to a standalone
[Native-AOT](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
executable named after the verb (`commit`, `push`, …). Shared logic lives in the
`Vcs.Flow.Common` library.

## Commands

| Command | Binary | What it does |
|---|---|---|
| `commit` | `commit` | Interactive stage-and-commit, with an optional push at the end. |
| `push` | `push` | Confirm and push the current branch (PR creation to follow). |

More commands are added the same way — one project per verb under `src/`.

## Requirements

- .NET 10 SDK (the exact band is pinned in [`global.json`](global.json)).
- `git` and/or `jj` on `PATH` at runtime.
- To produce the native executables: the platform C/C++ toolchain Native-AOT links
  against (clang + `zlib1g-dev` on Linux, the MSVC build tools on Windows, Xcode CLT
  on macOS).

## Building

```sh
dotnet build Vcs.Flow.slnx            # all commands + the shared library (warnings are errors)
dotnet test  Vcs.Flow.slnx            # run the test suite
dotnet publish src/Vcs.Flow.Commit/Vcs.Flow.Commit.csproj -c Release   # native `commit` binary
```

The published binary lands under
`src/Vcs.Flow.Commit/bin/Release/net10.0/<rid>/publish/commit`.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for the version history.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for build/test instructions and conventions.
To report a security issue, follow [SECURITY.md](SECURITY.md) — please do not open a
public issue.

## License

This project is licensed under the [MIT License](LICENSE).
