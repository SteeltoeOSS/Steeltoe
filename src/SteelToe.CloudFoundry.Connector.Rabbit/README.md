# CloudFoundry .NET Rabbit Connector

This project contains a SteelToe Connector for Rabbit.

## Provider Package Name and Feeds

`SteelToe.CloudFoundry.Connector.Rabbit`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Usage
You probably will want some understanding of how to use the [RabbitMq Client](https://www.rabbitmq.com/tutorials/tutorial-one-dotnet.html) before starting to use this connector. Also basic understanding of the `ConfigurationBuilder` and how to add providers to the builder is also helpful.

In order to use this Connector you need to do the following:
```
1. Create and bind a Rabbit Service instance to your application.
2. Optionally, configure any Rabbit client settings (e.g. appsettings.json)
3. Add SteelToe CloudFoundry config provider to your ConfigurationBuilder.
4. Add RabbitConnection to your ServiceCollection.
```

## Create & Bind Rabbit Service
You can create and bind Rabbit service instances using the CloudFoundry command line (i.e. cf):
```
1. cf target -o myorg -s myspace
2. cf create-service p-rabbitmq standard myRabbitService
3. cf bind-service myApp myRabbitService
4. cf restage myApp
```
Once you have bound the service to the app, the providers settings have been setup in `VCAP_SERVICES` and will be picked up automatically when the app is started by using the `CloudFoundry` configuration provider at startup.

## Optionally - Configure Rabbit Client Settings
Optionally you can configure the settings the Connector will use when setting up the RabbitConnection. Typically you would put these in your `appsettings.json` file and use the JSON configuration provider to add them to the applications configuration. Then when the Rabbit Connector configures the RabbitConnection it will the combine the settings from `appsettings.json` with the settings it obtains from the CloudFoundry configuration provider, with the CloudFoundry settings overriding any settings found in `appsettings.json`.

```
{
...
  "rabbit": {
    "client": {
      "uri": "amqp://guest:guest@127.0.0.1/"
    }
  }
  .....
}
```

 
For a complete list of client settings see the documentation in the `RabbitProviderConnectorOptions` file.

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

## Add RabbitConnector or a DbContext
The next step is to add RabbitConnector or DbContext's to your ServiceCollection depending on your needs.  You do this in `ConfigureServices(..)` method of the startup class:
```
#using SteelToe.CloudFoundry.Connector.Rabbit;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add RabbitConnector configured from CloudFoundry
        services.AddRabbitConnection(Configuration);

        // Add framework services.
        services.AddMvc();
        ...
    }
    ....
```

## Using Rabbit
Below is an example illustrating how to use the DI services to inject a `RabbitConnector` into a controller:


```
using Rabbit.Data.RabbitClient;
....
public class HomeController : Controller
{
    ...
    public IActionResult RabbitData([FromServices] RabbitConnection factory)
    {

        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            CreateQueue(channel);
            var body = Encoding.UTF8.GetBytes("a message");
            channel.BasicPublish(exchange: "",
                                 routingKey: "a-topic",
                                 basicProperties: null,
                                 body: body);
            }
        }
        return View();
    }
}


``` 
