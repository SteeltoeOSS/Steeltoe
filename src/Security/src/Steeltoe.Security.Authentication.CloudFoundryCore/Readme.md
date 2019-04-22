# ASP.NET Core Security Provider for CloudFoundry

This project contains a [ASP.NET Core External Security Providers](https://github.com/aspnet/Security) for CloudFoundry.

The providers simplify using CloudFoundry OAuth2 security services (e.g. [UAA Server](https://github.com/cloudfoundry/uaa) and/or [Pivotal Single Signon](https://docs.pivotal.io/p-identity/)) for Authentication and Authorization in an ASP.NET Core application.

There are two providers to choose from in this package:

* A provider that enables OAuth2 Single Signon with CloudFoundry Security services. Have a look at the Steeltoe [CloudFoundrySingleSignon](https://github.com/SteelToeOSS/Samples/tree/dev/Security/src/CloudFoundrySingleSignon) for a sample app.
* A provider that enables using JWT tokens issued by CloudFoundry Security services for securing REST endpoints. Have a look at the Steeltoe [CloudFoundryJwtAuthentication](https://github.com/SteelToeOSS/Samples/tree/dev/Security/src/CloudFoundryJwtAuthentication) for a sample app.

For more information on how to use this component see the online [Steeltoe documentation](http://steeltoe.io/).