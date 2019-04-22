# CloudFoundry .NET OAuth Connector

This project contains a Steeltoe Connector for OAuth services.  This connector simplifies using CloudFoundry OAuth2 security services (e.g. [UAA Server](https://github.com/cloudfoundry/uaa) or [Pivotal Single Signon](https://docs.pivotal.io/p-identity/)).

It exposes the OAuth service configuration data as injectable `IOption<OAuthServiceOptions>`. This connector is used by the ASP.NET Core [CloudFoundry External Security Provider](https://github.com/SteeltoeOSS/Security), but can be used standalone as well.

For more information on how to use this component see the online [Steeltoe documentation](http://steeltoe.io/).