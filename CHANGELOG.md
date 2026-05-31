# Changelog

All notable changes to **Vcs.Flow** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial repository scaffold: `Vcs.Flow.Common` shared library plus the `commit` and `push` command executables, each compiled to a Native-AOT binary.
- `Vcs.Flow.Common.VcsRunner` — ProcessKit-backed helper for invoking git/jj/gh and capturing output.
- `Vcs.Flow.Common.WorkingTree.ParsePorcelain` — parser for `git status --porcelain` output.

[Unreleased]: https://github.com/ZelAnton/vcs-flow-dotnet/commits/main
