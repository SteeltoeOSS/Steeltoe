# ASP.NET Core Security Provider for CloudFoundry

This project contains a [ASP.NET Core External Security Provider](https://github.com/aspnet/Security) for CloudFoundry. 

This provider simplifies using CloudFoundry OAuth2 security services (e.g. [UAA Server](https://github.com/cloudfoundry/uaa) and/or [Pivotal Single Signon](https://docs.pivotal.io/p-identity/)) for Authentication and Authorization in an ASP.NET Core application.

There are two providers to choose from in this package:
* A provider that enables OAuth2 Single Signon with CloudFoundry Security services. Have a look at the SteelToe [CloudFoundrySingleSignon](https://github.com/SteelToeOSS/Samples/tree/dev/Security/src/CloudFoundrySingleSignon) sample for details.
* A provider that enables using JWT tokens issued by CloudFoundry Security services for securing REST endpoints. Have a look at the SteelToe [CloudFoundryJwtAuthentication](https://github.com/SteelToeOSS/Samples/tree/dev/Security/src/CloudFoundryJwtAuthentication) sample for details.

## Package Name and Feeds

`SteelToe.Security.Authentication.CloudFoundry`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Basic Usage
You should have a good understanding of how the new .NET [Configuration model](http://docs.asp.net/en/latest/fundamentals/configuration.html) works and a basic understanding of the `ConfigurationBuilder` and how to add providers to the builder.  You should also have a good understanding of how the ASP.NET Core [Startup](https://docs.asp.net/en/latest/fundamentals/startup.html) class is used in configuring the application services and the middleware used in the app. Specfically pay particular attention to the usage of the `Configure` and `ConfigureServices` methods. And finally, you should have a good grasp of [ASP.NET Core Security](https://docs.asp.net/en/latest/security/).

With regard to CloudFoundry, you should have a good understanding of CloudFoundry OAuth2 security services (e.g. [UAA Server](https://github.com/cloudfoundry/uaa) and/or [Pivotal Single Signon](https://docs.pivotal.io/p-identity/)).

In order to use the Discovery client you need to do the following:
```
1. Create and bind an instance of a CloudFoundry OAuth2 service to your application.
2. Confiure any additional settings the Security provider will need. (Optional)
3. Add the CloudFoundry configuration provider to the ConfigurationBuilder.    
4. Add and Use the security provider in the application.
5. Secure your endpoints 
``` 
## Create & Bind OAuth2 Service
As mentioned above there are a couple OAuth2 services you can use on CloudFoundry. Rather than explaining the steps here, we recommend you read and follow the [Create OAuth2 Service Instance on CloudFoundry]() section of the SteelToe [CloudFoundrySingleSignon](https://github.com/SteelToeOSS/Samples/tree/dev/Security/src/CloudFoundrySingleSignon) sample.

Once you have bound the service to the app, the services settings have been setup in `VCAP_SERVICES` and will be picked up automatically when the app is started by using the `CloudFoundry` configuration provider discussed below during application startup.

## Additional Security provider settings (Optional)
Typically you do not need to configure any additional settings for the security provider.  In the below example, we show how to disable certificate validation for the security provider.  This is necessary when your app is targeted to run on Windows cells on CloudFoundry.
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
Next we add the CloudFoundry Configuration provider to the builder (e.g. `AddCloudFoundry()`). This is needed in order to pickup the `VCAP_` Service bindings and add them to the Configuration. Here is some sample code illustrating how this is done:

```
#using SteelToe.Extensions.Configuration;

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
## Add and Use the Security Provider
The next step is to Add and Use the Security provider.  You will do these two things in  `ConfigureServices(..)` and the `Configure(..)` methods of the `Startup` class. How you do this depends on which provider you wish to use.

## Using OAuth2 Single Signon 

The `AddCloudFoundryAuthentication()` call configures and adds the CloudFoundry authentication service to the ServiceCollection and the `UseCloudFoundryAuthentication()` call adds the CloudFoundry authentication middleware to the pipeline.
```
using SteelToe.Security.Authentication.CloudFoundry;
using SteelToe.Extensions.Configuration;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add CloudFoundry authentication service
        services.AddCloudFoundryAuthentication(Configuration);

        // Add framework services.
        services.AddMvc();
        ...
    }
    public void Configure(IApplicationBuilder app, ....)
    {
        ....
        app.UseStaticFiles();

        // Add CloudFoundry middleware to pipeline
        app.UseCloudFoundryAuthentication(new CloudFoundryOptions()
        {
                AccessDeniedPath = new PathString("/Home/AccessDenied")
        });

        app.UseMvc();

  
    }
    ....
```
## Using JWT Bearer Tokens

The `AddCloudFoundryJwtAuthentication()` call configures and adds the CloudFoundry authentication service to the ServiceCollection and the `UseCloudFoundryJwtAuthentication()` call adds the CloudFoundry authentication middleware to the pipeline.
```
using SteelToe.Security.Authentication.CloudFoundry;
using SteelToe.Extensions.Configuration;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add CloudFoundry authentication service
        services.AddCloudFoundryJwtAuthentication(Configuration);

        // Add framework services.
        services.AddMvc();
        ...
    }
    public void Configure(IApplicationBuilder app, ....)
    {
        ....
        app.UseStaticFiles();

        // Add CloudFoundry middleware to pipeline
        app.UseCloudFoundryJwtAuthentication();

        app.UseMvc();

  
    }
    ....
```
## Securing Endpoints - Authorize Attribute
Once you have the work done in your startup class you can secure endpoints using the standard ASP.NET Core `Authorize` attribute. You can do this in both ASP.NET Core Web API apps (i.e. using JWT Bearer token Security provider), and normal UI based apps.
```
using Microsoft.AspNetCore.Authentication;
....
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult About()
    {
        ViewData["Message"] = "Your About page.";
        return View();
    }
...
}
```
In the case above, if a user attempts to access the `About` endpoint and the user is not authenticated, then the user will be redirected to the OAuth2 server (e.g. UAA Server) to login and become authenticated.

## Securing Endpoints - Using Policies
You can also use ASP.NET Core Policies feature on the endpoints.  Here is an example of a Web API endpoint with the `testgroup` policy applied:
 ```
using Microsoft.AspNetCore.Authentication;
....
public class HomeController : Controller
{
[Route("api/[controller]")]
public class ValuesController : Controller
{
    // GET api/values
    [HttpGet]
    [Authorize(Policy = "testgroup")]
    public IEnumerable<string> Get()
    {
        return new string[] { "value1", "value2" };
    }

...
}
``` 
 
And here is what you need to do in your `Startup` class to create the `testgroup` policy:
```
using SteelToe.Security.Authentication.CloudFoundry;
using SteelToe.Extensions.Configuration;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add CloudFoundry authentication service
        services.AddCloudFoundryJwtAuthentication(Configuration);

        // Add testgroup policy mapping to claimtype 'scope'
        services.AddAuthorization(options =>
        {
            options.AddPolicy("testgroup", policy => policy.RequireClaim("scope", "testgroup"));
        });

        // Add framework services.
        services.AddMvc();
        ...
    }
    public void Configure(IApplicationBuilder app, ....)
    {
        ....
        app.UseStaticFiles();

        // Add CloudFoundry middleware to pipeline
        app.UseCloudFoundryJwtAuthentication();

        app.UseMvc();

  
    }
    ....
```
In the case above, if an incomming REST request is made to the `api/values` endpoint, and the request does not contain a valid JWT bearer token with a `scope` claim equal to `testgroup` the request will be rejected.
