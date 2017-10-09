# Steeltoe Common Packages

This repo contains several packages that are common to other Steeltoe components.  

Windows Master (Stable): [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/yw92sxmnol54d6oe/branch/master?svg=true)](https://ci.appveyor.com/project/steeltoe/common/branch/master)

Windows Dev (Less Stable): [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/yw92sxmnol54d6oe/branch/dev?svg=true)](https://ci.appveyor.com/project/steeltoe/common/branch/dev)

Linux/OS X Master (Stable): [![Travis Master](https://travis-ci.org/SteeltoeOSS/Common.svg?branch=master)](https://travis-ci.org/SteeltoeOSS/Common)

Linux/OS X Dev (Less Stable):  [![Travis Dev](https://travis-ci.org/SteeltoeOSS/Common.svg?branch=dev)](https://travis-ci.org/SteeltoeOSS/Common)

# Nuget Feeds

All new development is done on the dev branch. More stable versions of the packages can be found on the master branch. The latest prebuilt packages from each branch can be found on one of two MyGet feeds. Released version can be found on nuget.org.

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/. 

# Building Pre-requisites

To build and run the unit tests:

1. .NET Core SDK 2.0.0 or greater
2. .NET Core Runtime 2.0

# Building Packages & Running Tests - Windows

To build the packages on windows:

1. git clone ...
2. cd `<clone directory>`
3. dotnet restore --configfile nuget.config src
4. cd src\ `<project>` (e.g. cd src\Steeltoe.Common)
5. dotnet pack --configuration `<Release or Debug>` 

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src\Steeltoe.Common\bin)

To run the unit tests:

1. git clone ...
2. cd `<clone directory>`
3. dotnet restore --configfile nuget.config test
3. cd test\ `<test project>` (e.g. cd test\Steeltoe.Common.Test)
5. dotnet xunit -verbose

# Building Packages & Running Tests - Linux/OSX

To build the packages on Linux/OSX:

1. git clone ...
2. cd `<clone directory>`
3. dotnet restore --configfile nuget.config src
3. cd src/ `<project>` (e.g.. cd src/Steeltoe.Common)
5. dotnet pack --configuration `<Release or Debug>`

The resulting artifacts can be found in the bin folder under the corresponding project. (e.g. src/Steeltoe.Common/bin

To run the unit tests: 

1. git clone ...
2. cd `<clone directory>`
3. dotnet restore --configfile nuget.config test
4. cd test\ `<test project>` (e.g. cd test/Steeltoe.Common.Test)
5. dotnet xunit -verbose -framework netcoreapp2.0

