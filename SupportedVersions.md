# Steeltoe Supported Versions 
This document shows all versions of Steeltoe and their support lifecycle.

## Support Policy
Steeltoe follows the [VMware Tanzu OSS support policy](https://tanzu.vmware.com/support/oss) for critical bugs and security issues.

* Major versions will be supported for at least 3 years from the release date (but you must run a supported minor version).

* Minor versions will be supported for at least 12 months.


## Released Versions
The supported .NET Runtimes have been tested against the latest patch release of each version.  We recommend running the latest minor with the latest patch release for each major version.

### Currently Support Releases
The following table shows the releases that are currently supported:

| Steeltoe Version | Release Date       | End of Life Date | LTS | .NET Runtime Version  |
| ---------------- | ------------       | ---------------- | --- | --------------------  |
| 3.1.x            | In Progress        | TBD              | TBD | .NET Core 3.1 (LTS), .NET 5 |
| 3.0.x            | August 24, 2020    | August 31, 2022  | No  | .NET Core 3.1 (LTS), .NET 5 |
| 2.5.x            | October 15, 2020   | October 31, 2023 | Yes | .NET Standard 2.1, .NET Core 3.1(LTS) |
| 2.4.x            | November 13, 2019  | December 31, 2021| No  | .NET Standard 2.1, .NET Core 3.1(LTS) |
| 2.3.x            | August 21, 2019    | December 31, 2021| No  | .NET Standard 2.1, .NET Core 3.1(LTS) |
| 2.2.x            | March 15, 2019     | December 31, 2021| No  | .NET Standard 2.1, .NET Core 3.1(LTS) |
| 2.1.x            | August 18, 2018    | December 31, 2021| No  | .NET Standard 2.1, .NET Core 3.1(LTS) |
| 2.0.x            | February 15, 2018  | December 31, 2021| No  | .NET Standard 2.1, .NET Core 3.1(LTS) |


### End of Life Releases
The following table shows the releases that have reached end-of-life and no longer supported:

| Steeltoe Version | Release Date       | End of Life Date | LTS |
| ---------------- | ------------       | ---------------- | --- |
| 1.0.x            | April 7, 2017      | August 2019      | No  |
| 1.1.x            | June 19, 2017      | August 2019      | No  |

## Release Compatibility
The Steeltoe team is very aware of the need for stability and backward compatibility across our major releases (i.e. 2.x and 3.x). Our goal is to keep backward compatibility across all minor and maintenance releases (e.g. 3.1.3 would still work with 3.1.2, 3.1.1, 3.1.0).  For major releases, we reserve these releases to remove all deprecated code and make breaking changes when necessary for code optimization, enhancements, architecture changes, and redesign. We strive to keep all compatibility changes/breakages to major releases only .  There is a chance that we might be forced to break compatibility in a minor release, but it would be a rare occasion.

