# Creating and shipping Steeltoe releases

Due to breaking changes between release lines, portions of this document will not apply to release lines prior to `4.0.0`.

Please read the full contents of this page before taking action.

## Nuget package creation

All Steeltoe packages are produced by CI pipelines.
[This GitHub Actions workflow](https://github.com/SteeltoeOSS/Steeltoe/actions/workflows/package.yml) is used for building and releasing packages.

- Depending on how the workflow is triggered, packages may be signed, may deploy to the Steeltoe Azure Artifacts feed and may deploy to nuget.org.

To build packages for nuget.org and trigger their release, [create a new GitHub release](https://github.com/SteeltoeOSS/Steeltoe/releases/new) with a tag that matches the `VersionPrefix` in [shared-package.props](https://github.com/SteeltoeOSS/Steeltoe/blob/main/shared-package.props).
The workflow will handle everything else. **All deployments to nuget.org require manual approval by a senior member of the Steeltoe team.**

## Release-time checklist

- Run `public-api-mark-shipped.ps1` to update `PublicAPI.*.txt` files.
- Ensure [schema.json](https://github.com/SteeltoeOSS/Schema) is up to date. If necessary, create a git tag and copy the latest schema to [Documentation](https://github.com/SteeltoeOSS/Documentation).
- Review [documentation](https://github.com/SteeltoeOSS/Documentation) and [samples](https://github.com/SteeltoeOSS/Samples) for references to `github.com/SteeltoeOSS/Steeltoe` and ensure all links point to the proper branches.
- If there are pending updates for documentation (https://steeltoe.io): use the [stage-prod-swap](https://github.com/SteeltoeOSS/Documentation/actions/workflows/stage-prod-swap.yml) action to swap the staging slot into production.
- If there are pending updates for NetCoreToolTemplates, [create a new GitHub release](https://github.com/SteeltoeOSS/NetCoreToolTemplates/releases/new) to build and release new template versions to nuget.org.
  - **All deployments to nuget.org require manual approval by a senior member of the Steeltoe team.**

> [!NOTE]
> Updates to NetCoreToolTemplates will not automatically be used by start.steeltoe.io. A new build of NetCoreToolService will always be required because templates are embedded within the container image.

- Consider updating settings at SteeltoeOSS/InitializrConfig
- If there are pending updates for NetCoreToolService: use the [stage-prod-swap](https://github.com/SteeltoeOSS/NetCoreToolService/actions/workflows/stage-prod-swap.yml) action to swap the staging slot into production.
- If there are pending updates for InitializrService: use the [stage-prod-swap](https://github.com/SteeltoeOSS/InitializrService/actions/workflows/stage-prod-swap.yml) action to swap the staging slot into production.
- If there are pending updates for InitializrWeb (https://start.steeltoe.io): use the [stage-prod-swap](https://github.com/SteeltoeOSS/InitializrWeb/actions/workflows/stage-prod-swap.yml) action to swap the staging slot into production.

> [!TIP]
> For every case a "stage-prod-swap" workflow is run, consider using the corresponding "Build and stage xxx" to overwrite the retired production site (which was demoted to the corresponding staging slot) once the new version is confirmed as good to go.
