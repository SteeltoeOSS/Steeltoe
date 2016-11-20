# Steeltoe Discovery Client

This project contains the Steeltoe Discovery Client.  This client provides a generalized interface to service registries.  

Currently the client only supports [Spring Cloud Eureka Server](http://projects.spring.io/spring-cloud/docs/1.0.3/spring-cloud.html#spring-cloud-eureka-server), but in the future we will support additional service registries.

## Provider Package Name and Feeds

`Steeltoe.Discovery.Client`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Basic ASP.NET Core Usage
You should have a good understanding of how the new .NET [Configuration model](http://docs.asp.net/en/latest/fundamentals/configuration.html) works before starting to use the client. A basic understanding of the `ConfigurationBuilder` and how to add providers to the builder is necessary in order to configure the client.  You should also have a good understanding of how the ASP.NET Core [Startup](https://docs.asp.net/en/latest/fundamentals/startup.html) class is used in configuring the application services and the middleware used in the app. Specfically pay particular attention to the usage of the `Configure` and `ConfigureServices` methods. Its also important you have a good understanding of how to setup and use a Spring Cloud Eureka Server.  Detailed information on its usage can be found [here](http://projects.spring.io/spring-cloud/docs/1.0.3/spring-cloud.html#spring-cloud-eureka-server).

In order to use the Discovery client you need to do the following:
```
1. Confiure the settings the Discovery client will use to register servives in the service registry.
2. Configure the settings the Discovery client will use to discover services in the service registry.  
3. Add and Use the Discovery client as a service in the application.
``` 
## Eureka Settings needed to Register
Below is an example of the clients settings in JSON that are necessary to get the application to register a service named `fortuneService` with a Eureka Server at address `http://localhost:8761/eureka/`.  The `eureka:instance:port` setting is the port upon which the service can be found. The `eureka:client:shouldFetchRegistry` setting instructs the client not to fetch the registry as the app will not be needing to discover services; it only wants to register a service. The default for this property is true.

```
{
 "spring": {
    "application": {
      "name":  "fortuneService"
    }
  },
  "eureka": {
    "client": {
      "serviceUrl": "http://localhost:8761/eureka/",
      "shouldFetchRegistry": false
    },
    "instance": {
      "port": 5000
    }
  }
  .....
}
```
## Eureka Settings needed to Discover
Below is an example of the clients settings in JSON that are necessary to get the application to fetch the service registry from the Eureka Server at address `http://localhost:8761/eureka/` at startup.  The `eureka:client:shouldRegisterWithEureka` instructs the client to not register any services in the registry, as the app will not be offering up any services; it only wants to discover.

```
{
"spring": {
    "application": {
      "name": "fortuneUI"
    }
  },
  "eureka": {
    "client": {
      "serviceUrl": "http://localhost:8761/eureka/",
      "shouldRegisterWithEureka": false
    }
  }
  .....
}
```

For a complete list of client settings see the documentation in the [IEurekaClientConfig](https://github.com/SteeltoeOSS/Discovery/blob/master/src/Steeltoe.Discovery.Eureka.Client/IEurekaClientConfig.cs) and [IEurekaInstanceConfig](https://github.com/SteeltoeOSS/Discovery/blob/master/src/Steeltoe.Discovery.Eureka.Client/IEurekaInstanceConfig.cs) files.

## Add and Use the Discovery Client 
Once the providers settings have been defined and put in a file, the next step is to get them read in and make them available to the client. Using the C# example below, you can see that the clients configuration settings from above would be put in `appsettings.json` and the using the off-the-shelf JSON configuration provider we are able to read in the settings from the file using the provider (e.g. `AddJsonFile("appsettings.json")`.  

```
#using Steeltoe.Discovery.Client;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(IHostingEnvironment env)
    {
        // Set up configuration sources.
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            
            // Read in Discovery clients configuration
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }
    ....
```
The next step is to Add and Use the Discovery Client.  You do these two things in  `ConfigureServices(..)` and the `Configure(..)` methods of the startup class:
```
#using Steeltoe.Discovery.Client;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Steeltoe Discovery Client service
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
        
        // Use the Steeltoe Discovery Client service
        app.UseDiscoveryClient();
    }
    ....
```
## Discovering Services
Once the app has started, the Discovery client will begin to operate in the background, both registering services and periodically fetching the service registry from the server.

The simplest way of using the registry to lookup services when using the `HttpClient` is to use the Steeltoe `DiscoveryHttpClientHandler`. For example, see below the `FortuneService` class. It is intended to be used to retrieve Fortunes from a Fortune micro service. The micro service is registered under the name `fortuneService`.  

First, notice that the `FortuneService` constructor takes the `IDiscoveryClient` as a parameter. This is the discovery client interface which you use to lookup services in the service registry. Upon app startup, it is registered with the DI service so it can be easily used in any controller, view or service in your app.  Notice that the constructor code makes use of the client by creating an instance of the `DiscoveryHttpClientHandler`, giving it a reference to the `IDiscoveryClient`. 

Next, notice that when the `RandomFortuneAsync()` method is called, you see that the `HttpClient` is created with the handler. The handlers role is to intercept any requests made and evaluate the URL to see if the host portion of the URL can be resolved from the service registry.  In this case it will attempt to resolve the "fortuneService" name into an actual `host:port` before allowing the request to continue. If the name can't be resolved the handler will still allow the request to continue, but in this case, the request will fail.

Of course you don't have to use the handler, you can make lookup requests directly on the `IDiscoveryClient` interface if you need to.

```
using Steeltoe.Discovery.Client;
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
# Known Limitations

### Eureka Version
This client references a [Eureka 1.0 client](https://github.com/Netflix/eureka/wiki), not a 2.0 client. Eureka 2.0 is expected to have significant updates to its architecture and public API.

### Eureka AWS Support 
The Eureka client for Java contains features which enable operation on AWS.  The Steeltoe version does not currently implement those features, and instead, this version has been optimized for CloudFoundry environments. We will look at adding AWS cloud features at a future point in time.

### Eureak Configuration
Not all configuration properties found in the Java client are available for configuration. See [IEurekaClientConfig](https://github.com/SteeltoeOSS/Discovery/blob/master/src/Steeltoe.Discovery.Eureka.Client/IEurekaClientConfig.cs) and [IEurekaInstanceConfig](https://github.com/SteeltoeOSS/Discovery/blob/master/src/Steeltoe.Discovery.Eureka.Client/IEurekaInstanceConfig.cs) for configuration options.




