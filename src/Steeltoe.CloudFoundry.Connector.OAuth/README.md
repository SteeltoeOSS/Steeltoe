# CloudFoundry .NET OAuth Connector

This project contains a Steeltoe Connector for OAuth services.  This connector simplifies using CloudFoundry OAuth2 security services (e.g. [UAA Server](https://github.com/cloudfoundry/uaa) or [Pivotal Single Signon](https://docs.pivotal.io/p-identity/)).

It exposes the OAuth service configuration data as injectable `IOption<OAuthServiceOptions>`. This connector is used by the ASP.NET Core [CloudFoundry External Security Provider](https://github.com/SteeltoeOSS/Security), but can be used standalone as well.
## Provider Package Name and Feeds

`Steeltoe.CloudFoundry.Connector.OAuth`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Usage
You probably will want some understanding of CloudFoundry OAuth2 security services (e.g. [UAA Server](https://github.com/cloudfoundry/uaa) or [Pivotal Single Signon](https://docs.pivotal.io/p-identity/)) before starting to use this connector. Also basic understanding of the `ConfigurationBuilder` and how to add providers to the builder is also helpful.

In order to use this Connector you need to do the following:
```
1. Create and bind a OAuth service instance to your application.
2. Confiure any additional settings the OAuth connector will need. (Optional)
3. Add Steeltoe CloudFoundry configuration provider to your ConfigurationBuilder.
4. Add OAuth connector to your ServiceCollection.
5. Access the OAuth service options.
```
## Create & Bind OAuth Service

There are multiple OAuth services you can use on CloudFoundry. Rather than explaining the steps to create and bind the service to your app here, we recommend you read and follow the [Create OAuth2 Service Instance on CloudFoundry](https://github.com/SteeltoeOSS/Samples/tree/dev/Security/src/CloudFoundrySingleSignon) section of the Steeltoe [CloudFoundrySingleSignon](https://github.com/SteeltoeOSS/Samples/tree/dev/Security/src/CloudFoundrySingleSignon) sample.

Once you have bound the service to the app, the OAuth service settings have been setup in `VCAP_SERVICES` and will be picked up automatically when the app is started by using the `CloudFoundry` configuration provider at startup.

## Additional OAuth Connector settings (Optional)
Typically you do not need to configure any additional settings for the connector.  In the below example, we show how to add in the setting to disable certificate validation.  This might be necessary when your app is targeted to run on Windows cells on CloudFoundry and you are using self-signed certificates.
```
{
"Logging": {
    "IncludeScopes": false,
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    }
  },
"security": {
    "oauth2": {
      "client": {
        "validate_certificates": false
      }
    }
  }
  .....
}
```

## Add the CloudFoundry Configuration Provider
Next we add the Steeltoe CloudFoundry Configuration provider to the builder (e.g. `AddCloudFoundry()`). This is needed in order to pickup the `VCAP_` Service bindings and add them to the Configuration. Here is some sample code illustrating how this is done:

```
#using Steeltoe.Extensions.Configuration;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(IHostingEnvironment env)
    {
        // Set up configuration sources.
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddCloudFoundry();

        Configuration = builder.Build();
    }
    ....
```

## Add the OAuth Connector
The next step is to add OAuth connector to your ServiceCollection.  You do this in `ConfigureServices(..)` method of the `Startup` class:
```
#using Steeltoe.CloudFoundry.Connector.OAuth;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Configure and Add IOptions<OAuthServiceOptions> to the container
        services.AddOAuthServiceOptions(Configuration);

        // Add framework services.
        services.AddMvc();
        ...
    }
    ....
```
## Access OAuth Service options
Below is an example illustrating how to use the DI services to inject the OAuth service configuration data into a controller:


```
using Steeltoe.CloudFoundry.Connector.OAuth;
....
public class HomeController : Controller
{
    OAuthServiceOptions _options;

    public HomeController(IOptions<OAuthServiceOptions> oauthOptions)
    {
        _options = oauthOptions.Value;
    }
    ...
    public IActionResult OAuthOptions()
    {
        ViewData["ClientId"] = _options.ClientId;
        ViewData["ClientSecret"] = _options.ClientSecret;
        ViewData["UserAuthorizationUrl"] = _options.UserAuthorizationUrl;
        ViewData["AccessTokenUrl"] = _options.AccessTokenUrl;
        ViewData["UserInfoUrl"] = _options.UserInfoUrl;
        ViewData["TokenInfoUrl"] = _options.TokenInfoUrl;
        ViewData["JwtKeyUrl"] = _options.JwtKeyUrl;
        ViewData["ValidateCertificates"] = _options.ValidateCertificates;
        ViewData["Scopes"] = CommanDelimit(_options.Scope);

        return View();
    }
}
``` 
