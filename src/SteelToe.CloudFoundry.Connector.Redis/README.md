# CloudFoundry .NET Redis Connector

This project contain a SteelToe Connector for Redis.  This connector simplifies using [RedisCache](https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Redis) in an application running on CloudFoundry.

## Provider Package Name and Feeds

`SteelToe.CloudFoundry.Connector.Redis`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Usage
You probably will want some understanding of how to use the [RedisCache](https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Redis) before starting to use this connector. Also basic understanding of the `ConfigurationBuilder` and how to add providers to the builder is also helpful.

In order to use this Connector you need to do the following:
```
1. Create and bind a Redis Service instance to your application.
2. Optionally, configure any Redis client settings (e.g. appsettings.json)
3. Add SteelToe CloudFoundry config provider to your ConfigurationBuilder.
4. Add DistributedRedisCache to your ServiceCollection.
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

## Optionally - Configure Redis Client Settings
Optionally you can configure the settings the Connector will use when setting up the RedisCache. Typically you would put these in your `appsettings.json` file and use the JSON configuration provider to add them to the applications configuration. Then when the Redis Connector configures the RedisCache it will the combine the settings from `appsettings.json` with the settings it obtains from the CloudFoundry configuration provider, with the CloudFoundry settings overriding any settings found in `appsettings.json`.

```
{
...
  "redis": {
    "client": {
      "host": "http://foo.bar",
      "port": 1111
    }
  }
  .....
}
```

 
For a complete list of client settings see the documentation in the [RedisCacheConnectorOptions](https://github.com/SteelToeOSS/Connectors/blob/master/src/SteelToe.CloudFoundry.Connector.Redis/RedisCacheConnectorOptions.cs) file.

## Add the CloudFoundry Configuration Provider
Next we add the CloudFoundry Configuration provider to the builder (e.g. `AddCloudFoundry()`). This is needed in order to pickup the VCAP_ Service bindings and add them to the Configuration. Here is some sample code illustrating how this is done:
```
#using SteelToe.Extensions.Configuration;
...

var builder = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()                   
    .AddCloudFoundry();
          
var config = builder.Build();
...

```
Normally in an ASP.NET Core application, the above C# code is would be included in the constructor of the `Startup` class. For example, you might see something like this:
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

            // Add to configuration the Cloudfoundry VCAP settings
            .AddCloudFoundry();

        Configuration = builder.Build();
    }
    ....
```

## Add the DistributedRedisCache
The next step is to Add DistributedRedisCache.  You do this in `ConfigureServices(..)` method of the startup class:
```
#using SteelToe.CloudFoundry.Connector.Redis;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Microsoft Redis Cache configured from CloudFoundry
        services.AddDistributedRedisCache(Configuration);

        // Add framework services.
        services.AddMvc();
        ...
    }
    ....
```
## Using the Cache
Below is an example illustrating how to use the DI services to inject the Cache into a controller:


```
using Microsoft.Extensions.Caching.Distributed;
....
public class HomeController : Controller
{
    private IDistributedCache _cache;
    public HomeController(IDistributedCache cache)
    {
        _cache = cache;
    }
    ...
    public IActionResult CacheData()
    {
        string key1 = Encoding.Default.GetString(_cache.Get("Key1"));
        string key2 = Encoding.Default.GetString(_cache.Get("Key2"));

        ViewData["Key1"] = key1;
        ViewData["Key2"] = key2;

        return View();
    }
}
``` 
