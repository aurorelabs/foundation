# Publishing

The version of a package is the line in [`Versions.props`](../../Versions.props), published exactly as written. Nothing is bumped by a pipeline.

- A change under `src/<Package>` goes with a change of that package's version in the same pull request, and the other way round. The pull request check fails otherwise.
- A preview is a version with a suffix: `0.2.0-preview.1`, then `0.2.0-preview.2`.
- Merging to `main` builds, tests and pushes every package whose `src` changed, each independently, to GitHub Packages (`https://nuget.pkg.github.com/aurorelabs/index.json`).
  A version that already exists fails that package only: change the version and run the workflow again.
- One GitHub release lists what was published in the run, with the `Unreleased` notes of the changelog. It is a pre-release when everything published is a preview.
- **Cleanup old package versions** (manual) deletes old versions, keeping the newest N per package for stable and preview separately. It lists first unless dry-run is turned off.

The pull request check also validates the commit messages (Conventional Commits), `dotnet format --verify-no-changes` and the tests with a coverage summary.
The tests of `EntityFrameworkCore.Postgres` run against a PostgreSQL service container; see [Testing](./testing.md) for local runs.
