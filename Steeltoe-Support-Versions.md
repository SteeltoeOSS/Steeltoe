# Steeltoe Supported Versions

This page lists published Steeltoe versions, the support policy (4.0+ is the default below), a table of release and end-of-life dates, and .NET runtimes we validated.

## For Steeltoe 4.0 and newer

This section is the support policy for **4.x and later** (current and future work).

**How long a major is supported** — A **major** line (for example, all 4.x) is supported for at least **three years** from the **date that major was first released**, and for at least **one year** after the **date the next major** is released. That major **ends** no **earlier** than the **later** of those two dates.

**Security and serious-bug fixes** — We add these only in **new patch** releases of the **newest** **minor** in a **supported** **major** (for example, 4.2.x when 4.2 is the newest 4.x minor, not 4.0.x or 4.1.x). You must use the **latest patch** of that **newest** minor. We do **not** add new **security** patches to **older** minors. **Upgrade** the **minor** to the current line, then to the **latest patch**, to receive these fixes.

**Pre-release and go-live** — Go-live releases that we support in production are often **release-candidate** builds just before **GA**. Pre-GA and go-live **packages** are not in the same **security** patch **policy** as this section unless we **announce** that they are.

**Compatibility** — We try to keep compatibility across minors and patch releases within a major. Breaking changes are normally reserved for a new major. A rare break in a minor is possible; we will explain it. Security and serious-bug fixes follow the “Security and serious-bug fixes” section above, not a promise of matching patches on every older minor.

## For Steeltoe 3.x and older

The [For Steeltoe 4.0 and newer](#for-steeltoe-40-and-newer) section applies only to 4.0 and later. It does not change or replace the end-of-life dates in the [Release support matrix](#release-support-matrix) for 3.x, 2.x, 1.x, or any earlier line. For those versions, the matrix is what we have published. Use the dates and .NET runtime columns in the table for older rows; the 4.0+ policy does not override them.

## Release support matrix

When we release a version, we test it with the .NET runtimes in good standing at the time. Prefer the latest supported Steeltoe and .NET patch versions, consistent with the 4.0+ policy and [.NET runtime support](#net-runtime-support) below.

| Steeltoe Version | Release Date       | End of Life Date | .NET Runtime Version  |
| ---------------- | ------------       | ---------------- | --------------------  |
| 4.1.0            | January 30, 2026   | January 2027     | .NET 8 - 10 | 
| 4.0.0            | September 4, 2025  | September 2028   | .NET 8 - 9 | 
| 3.3.0            | September 4, 2025  | September 2026   | .NET 6 - 8* | 
| 3.2.0            | May 26, 2022       | May 26, 2023     | .NET Core 3.1, .NET 5 - 6 |
| 3.1.0            | July 13, 2021      | July 31, 2022    | .NET Core 3.1, .NET 5 |
| 3.0.0            | August 21, 2020    | August 31, 2023  | .NET Core 3.1 |
| 2.5.0            | October 1, 2020    | October 31, 2023 | .NET Framework 4.6.2 - 4.8, .NET Core 2.1 - 3.1 |
| 2.4.0            | November 13, 2019  | December 31, 2021| .NET Framework 4.6.1 - 4.8, .NET Core 2.1 - 3.1 |
| 2.3.0            | August 21, 2019    | December 31, 2021| .NET Framework 4.6.1 - 4.8, .NET Core 2.1 - 3.1 |
| 2.2.0            | March 15, 2019     | December 31, 2021| .NET Framework 4.6.1 - 4.8, .NET Core 2.1 - 3.1 |
| 2.1.0            | August 17, 2018    | December 31, 2021| .NET Framework 4.6.1 - 4.8, .NET Core 2.0 - 3.1 |
| 2.0.0            | February 15, 2018  | December 31, 2021| .NET Framework 4.6.1 - 4.8, .NET Core 2.0 - 3.1 |
| 1.1.0            | September 15, 2017 | August 31, 2019  | .NET Framework 4.5.2 - 4.7, .NET Core 1.0 - 1.1 |
| 1.0.0            | March 31, 2017     | August 31, 2019  | .NET Framework 4.5.2 - 4.6.2, .NET Core 1.0 - 1.1 |

\* Integration, Messaging and Stream are supported on .NET 6 only.

> [!NOTE]
> For each release, we list the original release date, along with the .NET versions supported at that moment in time.
> These are the combinations that were confirmed to work when the release was created.
> If newer .NET versions emerge before the end-of-life date, we don't list them here.
> That doesn't mean they won't work or won't be supported, only that they aren't automatically guaranteed.

## .NET runtime support

Steeltoe libraries depend on .NET runtimes, so we follow the [.NET and .NET Core Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core).
Once a runtime is out of support, Steeltoe will also discontinue support for running our libraries on those runtime versions.
