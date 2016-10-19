# ASP.NET Core Redis Key Storage Provider for CloudFoundry

This project contains a [ASP.NET Core Redis Key Storage Provider](https://docs.asp.net/en/latest/security/data-protection/implementation/key-storage-providers.html) for CloudFoundry. 

This provider simplifies using Redis on CloudFoundry as a custom key repository.

## Package Name and Feeds

`Steeltoe.Security.DataProtection.Redis`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Basic Usage
You should have a good understanding of how .NET [DataProtection](https://docs.asp.net/en/latest/security/data-protection/index.html) works and is used throughout ASP.NET Core.  You should also have a good understanding of how the ASP.NET Core [Startup](https://docs.asp.net/en/latest/fundamentals/startup.html) class is used in configuring the application services. Specfically pay particular attention to the usage of the `ConfigureServices` method.

In order to use this provider you need to do the following:
```
1. Create and bind a Redis Service instance to your application.
2. Add Steeltoe CloudFoundry config provider to your ConfigurationBuilder.
3. Add Redis ConnectionMultiplexer to your ServiceCollection.
4. Add DataProtection to your ServiceCollection & configure it to PersistKeysToRedis
``` 
## Create & Bind Redis Service
You can create and bind Redis service instances using the CloudFoundry command line (i.e. cf):
```
1. cf target -o myorg -s myspace
2. cf create-service p-redis shared-vm myRedisCache
3. cf bind-service myApp myRedisCache
4. cf restage myApp
```
Once you have bound the service to the app, the providers settings have been setup in `VCAP_SERVICES` and will be picked up automatically when the app is started by using the `CloudFoundry` configuration provider at startup.

## Add the CloudFoundry Configuration Provider
Next we add the CloudFoundry Configuration provider to the builder (e.g. `AddCloudFoundry()`). This is needed in order to pickup the `VCAP_` Service bindings and add them to the Configuration. Here is some sample code illustrating how this is done:

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
## Add the ConnectionMultiplexer 
The next step is to add ConnectionMultiplexer to your ServiceCollection.  You do this in `ConfigureServices(..)` method of the startup class:
```
#using Steeltoe.CloudFoundry.Connector.Redis;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add StackExchange ConnectionMultiplexor configured from CloudFoundry
        services.AddRedisConnectionMultiplexer(Configuration);

        // Add framework services.
        services.AddMvc();
        ...
    }
    ....
```
## Add DataProtection & PersistKeysToRedis
The next step is to add DataProtection to your ServiceCollection and configure it to persist the keys to Redis.  You do this in `ConfigureServices(..)` method of the startup class:
```
#using Steeltoe.CloudFoundry.Connector.Redis;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add StackExchange ConnectionMultiplexor configured from CloudFoundry
        services.AddRedisConnectionMultiplexer(Configuration);

        // Add DataProtection and persist keys to CloudFoundry Redis service
        services.AddDataProtection()
            .PersistKeysToRedis()
            .SetApplicationName("Some Name");

        // Add framework services.
        services.AddMvc();
        ...
    }
    ....
```
## Using Shared KeyStore
Once this has been setup, the keys used by the DataProtection framework will be stored in the bound Redis CloudFoundry service. As an example of how this can be leveraged, imagine you want to share protected data stored in a Session across instances of an ASP.NET Core application running on CloudFoundry. 

To do this, we first need to configure the Session feature in our application. To do this, we add the following to the Startup class we already have created above:
```
#using Steeltoe.CloudFoundry.Connector.Redis;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add StackExchange ConnectionMultiplexor configured from CloudFoundry
        services.AddRedisConnectionMultiplexer(Configuration);

        // Add DataProtection and persist keys to CloudFoundry Redis service
        services.AddDataProtection()
            .PersistKeysToRedis()
            .SetApplicationName("Some Name");

        // Add IDistributedCache to container configured from CloudFoundry
        services.AddDistributedRedisCache(Configuration);

        // Add Session feature to container, it will use IDistributedCache to store session data
        // and it will also use the DataProtection provider configured above when encrypting the 
        // Session cookie it returns to the browser
        services.AddSession();

        // Add framework services.
        services.AddMvc();
        ...
    }
    ....
```

Once you have done this, then in a Controller you can inject `IDataProtectionProvider` and use it to protect any data you store in Session.

Since the keys and the session data are stored in the bound Redis cache, you should be able to scale the app (e.g. `cf scale myApp -i 5`) and find that the session data is available to all instances.
```
    public class HomeController : Controller
    {
        IDataProtectionProvider _protection;
        private IHttpContextAccessor _httpContext;

        public HomeController(IDataProtectionProvider protection, IHttpContextAccessor contextAccessor)
        {
            _protection = protection;
            _httpContext = contextAccessor;
        }
        ...
        public async Task<IActionResult> Protected()
        {
            var session = _httpContext.HttpContext.Session;

            string protectedString = session.GetString("SomethingProtected");
            if (string.IsNullOrEmpty(protectedString)) {
                protectedString = "My Protected String - " + Guid.NewGuid().ToString();
                session.SetString("SomethingProtected",
                    _protection.CreateProtector("MyProtectedData in Session").Protect(protectedString));
                await session.CommitAsync();
            } else
            {
                protectedString = _protection.CreateProtector("MyProtectedData in Session").Unprotect(protectedString);
            }

            ViewData["SessionID"] = session.Id;
            ViewData["SomethingProtected"] = protectedString;
            ViewData["InstanceIndex"] = GetInstanceIndex();
            return View();
        }

```
