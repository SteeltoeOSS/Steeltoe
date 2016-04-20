# Configuration

AppVeyor Master:  [![AppVeyor Master](https://ci.appveyor.com/api/projects/status/27c2hd0460aac1cs/branch/master?svg=true)](https://ci.appveyor.com/project/steeltoe/Configuration)
AppVeyor Dev:  [![AppVeyor Dev](https://ci.appveyor.com/api/projects/status/27c2hd0460aac1cs/branch/dev?svg=true)](https://ci.appveyor.com/project/steeltoe/Configuration)
Travis Master: [![Travis Master](https://travis-ci.org/SteelToeOSS/Configuration.svg?branch=master)](https://travis-ci.org/SteelToeOSS/Configuration)
Travus Dev: [![Travis Dev](https://travis-ci.org/SteelToeOSS/Configuration.svg?branch=dev)](https://travis-ci.org/SteelToeOSS/Configuration)

# Building

To build this solution, go to the root of the repository (make sure it's freshly checked out with no leftovers) and issue the following command:

`$ dnu restore`

This will fetch all the necessary dependencies and deal with the dependencies between the various projects in the solution, including allowing the samples to reference the SteelToe configuration assemblies. In its current state, you may see an issue with a test project not being able to see one of the other projects. This can be ignored (for now).

Once you've done a restore, you can build one of the samples:

`$ dnu build samples/Simple/src/Simple/ --framework dnxcore5`

Depending on how messed up your local installation of the RC1 bits are, you may or may not need to use `sudo` (Some of us have ownership problems with the lock files produced during compilation).

Once the sample has been built, you can run it:

```
$ cd samples/Simple/src/Simple
$ dnx web
Hosting environment: Production
Now listening on: http://localhost:5000
Application started. Press Ctrl+C to shut down.
```
This sample assumes that there is a running Spring Cloud Config Server on your machine. To make this happen:

1. Clone the Spring Cloud Config Server repository. (https://github.com/spring-cloud/spring-cloud-config)
2. Go to the config server directory (`spring-cloud-config/spring-cloud-config-server`) and fire it up with `mvn spring-boot:run`
3. The SteelToe sample will default to looking for its spring cloud config server on localhost, so it should all connect now.

With the ASP.NET Core app running, you can navigate to the `Spring Cloud Data` tab and you'll see the values stored in the github repo used for the Spring Cloud Config Server samples.
