# Spring Cloud Config .NET Configuration Provider

This project contains the [Spring Cloud Config Server](http://projects.spring.io/spring-cloud/docs/1.0.3/spring-cloud.html#_spring_cloud_config) client configuration provider.  By acting as a client to the Spring Cloud Config Server, this provider enables the Config Server to become a source of configuration data for a .NET application.  You can learn more about Cloud Native Applications and the Spring Cloud Config Server at [Spring Cloud](http://projects.spring.io/spring-cloud/docs/1.0.3/spring-cloud.html).

## Provider Package Name and Feeds

`SteelToe.Extensions.Configuration.ConfigServer`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Basic Usage
You should have a good understanding of how the new .NET [Configuration model](http://docs.asp.net/en/latest/fundamentals/configuration.html) works before starting to use this provider. A basic understanding of the `ConfigurationBuilder` and how to add providers to the builder is necessary. Its also important you have a good understanding of how to setup and use a Spring Cloud Config Server.  Detailed information on its usage can be found [here](http://projects.spring.io/spring-cloud/docs/1.0.3/spring-cloud.html#_spring_cloud_config_server).

In order to retrieve configuration data from the Config Server you need to do the following:
```
1. Configure the settings the provider will use when contacting the Config Server.  
2. Add the Confg Server provider to the Configuration builder.
``` 
## Configure Provider Settings & Add Provider to Builder
The most convenient way to configure the settings is to provide them in a file and then use one of the other file based configuration providers to read them in and make them available.  Below is an example of the providers settings in JSON. Only two settings are provided; the setting `spring:application:name` configures the "application name" to `foo`, and `spring:cloud:config:uri` configures the address of the Config Server.  

For a complete list of provider settings see the documentation in the [ConfigServerClientSettings](https://github.com/SteelToeOSS/Configuration/blob/master/src/SteelToe.Extensions.Configuration.ConfigServer/ConfigServerClientSettings.cs) file. For an understanding of how these settings and others are used in retrieving configuration data from the Config Server, see the Spring Cloud Config Client/Server [documentation](http://projects.spring.io/spring-cloud/docs/1.0.3/spring-cloud.html#_spring_cloud_config). 
```
{
  "spring": {
    "application": {
      "name": "foo"
    },
    "cloud": {
      "config": {
        "uri": "http://localhost:8888"
      }
    }
  }
  .....
}
```
Once the providers settings have been defined and put in a file, the next step is to get them read in and made available to the provider. In the C# example below, the providers configuration settings from above would be contained in the `appsettings.json` file.  By using the off-the-shelf JSON file provider we are able to read the settings from the file simply by adding the provider to the configuration builder (e.g. `AddJsonFile("appsettings.json")`.  

Next we add the Config Server provider to the builder (e.g. `AddConfigServer()`). Because the JSON provider is added `before` the Config Server provider, the JSON based settings will become available to the provider.  Note that you don't have to use JSON for the providers settings, you can use any of the other off-the-shelf configuration providers for the settings (e.g. INI file, environment variables, etc.).  The important thing to understand is you need to `Add*()` the source of the settings (i.e. `AddJsonFile(..)`) BEFORE you `AddConfigServer(..)`,  otherwise the settings won't be picked up and used.
```
#using SteelToe.Extensions.Configuration;
...

var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()                   
    .AddConfigServer();
          
var config = builder.Build();
...
```
Normally in an ASP.NET 5 application, the above C# code is would be included in the constructor of the `Startup` class. For example, you might see something like this:
```
#using SteelToe.Extensions.Configuration;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
        // Set up configuration sources.
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddConfigServer();

        Configuration = builder.Build();
    }
    ....
```
## Accessing Configuration Data
Using the example code above, when the `buider.Build()` method is called, the Config Server provider will make the appropriate REST call(s) to the Config Server and retrieve configuration values based on the settings that have been provided in `appsettings.json`.

Once the configuration is built you can then access the retrieved configuration data directly from the configuration as follows:
```
....
var config = builder.Build();
var property1 = config["myconfiguration:property1"]
var property2 = config["myconfiguration:property2"] 
....
```
Alternatively you can use the [Options](https://github.com/aspnet/Options) framework together with [Dependency Injection](http://docs.asp.net/en/latest/fundamentals/dependency-injection.html) provided in ASP.NET 5 for accessing the configuration data as POCOs.
To do this, first create a POCO to represent your configuration data from the Config Server. For example:
```
public class MyConfiguration {
    public string Property1 { get; set; }
    public string Property2 { get; set; }
}
```
Then add the the following to your `public void ConfigureServices(...)` method in your `Startup` class.
```
public void ConfigureServices(IServiceCollection services)
{
    // Setup Options framework with DI
    services.AddOptions();
    
    // Configure IOptions<MyConfiguration> 
    services.Configure<MyConfiguration>(Configuration);
    ....
}
```
The `Configure<MyConfiguration>(Configuration)` binds the `myconfiguration:...` configuration values to an instance of `MyConfiguration`. After this you can then gain access to this POCO in Controllers or Views via Dependency Injection.  Here is an example controller illustrating this:
```

public class HomeController : Controller
{
    public HomeController(IOptions<MyConfiguration> myOptions)
    {
        MyOptions = myOptions.Value;
    }

    MyConfiguration MyOptions { get; private set; }

    // GET: /<controller>/
    public IActionResult Index()
    {
        ViewData["property1"] = MyOptions.Property1;
        ViewData["property2"] = MyOptions.Property2;
        return View();
    }
}
```


