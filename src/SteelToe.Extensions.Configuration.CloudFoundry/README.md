# CloudFoundry .NET Configuration Provider

This project contains the CloudFoundry configuration provider.  This provider enables the CloudFoundry environment variables, `VCAP_APPLICATION` and `VCAP_SERVICES` to be parsed and accessed as configuration data within a .NET application.

# Package Name and Feeds

`SteelToe.Extensions.Configuration.CloudFoundry`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

# Usage
You should have a good understanding of how the [new Configuration model](http://docs.asp.net/en/latest/fundamentals/configuration.html) works before starting to use this provider. A basic understanding of the `ConfigurationBuilder` and how to add providers to the builder is necessary.

In order to get the `VCAP_*` environment variables parsed and made available in your configuration you need to add the CloudFoundry configuration provider to the builder.

```
var builder = new ConfigurationBuilder();
builder.Add******()                    // Add any addtional providers(e.g. JSON, etc) either before or after adding the CloudFoundry provider
builder.AddCloudFoundry();      
var config = builder.Build();
```
Upon completion of the `buider.Build()` method call, the values from `VCAP_APPLICATION` and `VCAP_SERVICES` will be available under the keys of `vcap:application` and `vcap:services` respectively.

In an ASP.NET 5 application, the above code is normally included in the constructor of the `Startup` class. For example, you might see something like this:

```
#using SteelToe.Extensions.Configuration;

public class Startup {
    .....
    public Startup(...)
    {
        // Set up configuration sources.
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddCloudFoundry();

        Configuration = builder.Build();
    }
    ....
```
Once the configuration is built you can access the [VCAP_APPLICATION environment variable](http://docs.cloudfoundry.org/devguide/deploy-apps/environment-variable.html#VCAP-APPLICATION) data directly from the configuration as follows:
```
var config = builder.Build();
var appName = config["vcap:application:application_name"]
var instaneId = config["vcap:application:instance_id"] 
....
```
You can access the [VCAP_SERVICES environment variables](http://docs.cloudfoundry.org/devguide/deploy-apps/environment-variable.html#VCAP-SERVICES) data directly from the configuration as follows:
```
var config = builder.Build();
var mySqlName = config["vcap:services:p-mysql:0:name"]
var instaneId = config["vcap:application:p-mysql:0:credentials:uri"] 
....
```
Note: The provider uses the built-in [JSON configuration provider](https://github.com/aspnet/Configuration/tree/dev/src/Microsoft.Extensions.Configuration.Json) in parsing the JSON in the `VCAP_*` environment variables.

Alternatively you can use the [Options](https://github.com/aspnet/Options) framework together with [Dependency Injection](http://docs.asp.net/en/latest/fundamentals/dependency-injection.html) provided in ASP.NET 5 for accessing configuration data as POCOs.

To do this, add the the following to your `public void ConfigureServices(...)` method in your `Startup` class.
```
#using SteelToe.Extensions.Configuration.CloudFoundry;

public void ConfigureServices(IServiceCollection services)
{
    // Setup options with DI
    services.AddOptions();
    
    // Configure IOptions<CloudFoundryApplicationOptions> & IOptions<CloudFoundryServicesOptions> 
    services.Configure<CloudFoundryApplicationOptions>(Configuration);
    services.Configure<CloudFoundryServicesOptions>(Configuration);
}
```
The `Configure<CloudFoundryApplicationOptions>(Configuration)` binds the `vcap:application:...` configuration values to an instance of `CloudFoundryApplicationOptions` and the `Configure<CloudFoundryServicesOptions>(Configuration)` binds the `vcap:services:...` config values to an instance of `CloudFoundryServicesOptions`. After this is done you can then gain access to these POCOs in Controllers or Views via Dependency Injection.  Here is an example controller illustrating this:
```
#using SteelToe.Extensions.Configuration.CloudFoundry;

public class HomeController : Controller
{
    public HomeController(IOptions<CloudFoundryApplicationOptions> appOptions, IOptions<CloudFoundryServicesOptions> serviceOptions )
    {
        AppOptions = appOptions.Value;
        ServiceOptions = serviceOptions.Value;
    }

    CloudFoundryApplicationOptions AppOptions { get; private set; }
    CloudFoundryServicesOptions ServiceOptions { get; private set; }

    // GET: /<controller>/
    public IActionResult Index()
    {
        ViewData["AppName"] = AppOptions.ApplicationName;
        ViewData["AppId"] = AppOptions.ApplicationId;
        ViewData["URI-0"] = AppOptions.ApplicationUris[0];
        
        ViewData[ServiceOptions.Services[0].Label] = ServiceOptions.Services[0].Name;
        ViewData["client_id"]= ServiceOptions.Services[0].Credentials["client_id"].Value;
        ViewData["client_secret"]= ServiceOptions.Services[0].Credentials["client_secret"].Value;
        ViewData["uri"]= ServiceOptions.Services[0].Credentials["uri"].Value;
        return View();
    }
}
```

