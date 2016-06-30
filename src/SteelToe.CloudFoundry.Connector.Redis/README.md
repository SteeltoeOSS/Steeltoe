# CloudFoundry .NET Redis Connector

This project contain a SteelToe Connector for Redis.  This connector simplifies using [RedisCache](https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Redis) in an application running on CloudFoundry.

## Provider Package Name and Feeds

`SteelToe.CloudFoundry.Connector.Redis`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Usage
You should have some understanding of how to use the [RedisCache](https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Redis) before starting to use this connector. Also basic understanding of the `ConfigurationBuilder` and how to add providers to the builder is also helpful.

In order to use this Connector you need to do the following:
```
1. Create and bind a Redis Service instance to your application.
2. Optionally, configure any Redis client settings you need in your applications configuration (e.g. appsettings.json)
3. Add SteelToe CloudFoundry provider to your ConfigurationBuilder.
4. Add DistributedRedisCache to your ServiceCollection.
```
## Create & Bind Redis Service
You can create and bind Redis service instances using the CloudFoundry command line (i.e. cf).
```
1. cf target -o myorg -s myspace
2. cf create-service p-redis shared-vm myRedisCache
3. cf bind-service myApp myRedisCache
4. cf restage myApp
```
Once you have bound the service to the app, the providers settings have been setup in `VCAP_SERVICES` and will be picked up automatically when the app is started by using the `CloudFoundry` configuration provider at startup.

## Optionally - Configure Redis Client Settings

```
{
...
  "redis": {
    "client": {
      ... Add settings
    }
  }
  .....
}
```

For a complete list of client settings see the documentation in the [IEurekaClientConfig](https://github.com/SteelToeOSS/Discovery/blob/master/src/SteelToe.Discovery.Eureka.Client/IEurekaClientConfig.cs) and [IEurekaInstanceConfig](https://github.com/SteelToeOSS/Discovery/blob/master/src/SteelToe.Discovery.Eureka.Client/IEurekaInstanceConfig.cs) files.

## Add the CloudFoundry Configuration Provider
Next we add the CloudFoundry Configuration provider to the builder (e.g. `AddCloudFoundry()`). This is needed in order to pickup the VCAP_ Service bindings and add them to the Configuration. Here is some sample code illustrating how this is done:
```
#using Pivotal.Extensions.Configuration;
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
#using Pivotal.Extensions.Configuration;

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
## Add and Use the Discovery Client 
The next step is to Add and Use the Discovery Client.  You do these two things in  `ConfigureServices(..)` and the `Configure(..)` methods of the startup class:
```
#using Pivotal.Discovery.Client;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Pivotal Discovery Client service
        services.AddDiscoveryClient(Configuration);

        // Add framework services.
        services.AddMvc();
        ...
    }
    public void Configure(IApplicationBuilder app, ....)
    {
        ....
        app.UseStaticFiles();
        app.UseMvc();
        
        // Use the Pivotal Discovery Client service
        app.UseDiscoveryClient();
    }
    ....
```
## Discovering Services
Once the app has started, the Discovery client will begin to operate in the background, both registering services and periodically fetching the service registry from the server.

The simplest way of using the registry to lookup services when using the `HttpClient` is to use the Pivotal `DiscoveryHttpClientHandler`. For example, see below the `FortuneService` class. It is intended to be used to retrieve Fortunes from a Fortune micro service. The micro service is registered under the name `fortuneService`.  

First, notice that the `FortuneService` constructor takes the `IDiscoveryClient` as a parameter. This is the discovery client interface which you use to lookup services in the service registry. Upon app startup, it is registered with the DI service so it can be easily used in any controller, view or service in your app.  Notice that the constructor code makes use of the client by creating an instance of the `DiscoveryHttpClientHandler`, giving it a reference to the `IDiscoveryClient`. 

Next, notice that when the `RandomFortuneAsync()` method is called, you see that the `HttpClient` is created with the handler. The handlers role is to intercept any requests made and evaluate the URL to see if the host portion of the URL can be resolved from the service registry.  In this case it will attempt to resolve the "fortuneService" name into an actual `host:port` before allowing the request to continue. If the name can't be resolved the handler will still allow the request to continue, but in this case, the request will fail.

Of course you don't have to use the handler, you can make lookup requests directly on the `IDiscoveryClient` interface if you need to.

```
using Pivotal.Discovery.Client;
....
public class FortuneService : IFortuneService
{
    DiscoveryHttpClientHandler _handler;
    private const string RANDOM_FORTUNE_URL = "http://fortuneService/api/fortunes/random";
    public FortuneService(IDiscoveryClient client)
    {
        _handler = new DiscoveryHttpClientHandler(client);
    }
    public async Task<string> RandomFortuneAsync()
    {
        var client = GetClient();
        return await client.GetStringAsync(RANDOM_FORTUNE_URL);
    }
    private HttpClient GetClient()
    {
        var client = new HttpClient(_handler, false);
        return client;
    }
}
``` 
