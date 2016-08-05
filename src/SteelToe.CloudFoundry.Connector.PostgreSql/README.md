# CloudFoundry .NET Postgres Connector

This project contains a SteelToe Connector for Postgres.  This connector simplifies using [Npgsql - 3.1.5](http://www.npgsql.org/) in an application running on CloudFoundry.

## Provider Package Name and Feeds

`SteelToe.CloudFoundry.Connector.PostgreSql`

[Development feed (Less Stable)](https://www.myget.org/gallery/steeltoedev) - https://www.myget.org/gallery/steeltoedev

[Master feed (Stable)](https://www.myget.org/gallery/steeltoemaster) - https://www.myget.org/gallery/steeltoemaster

[Release or Release Candidate feed](https://www.nuget.org/) - https://www.nuget.org/

## Usage
You probably will want some understanding of how to use the [Npgsql - 3.1.5](http://www.npgsql.org/) before starting to use this connector. Also basic understanding of the `ConfigurationBuilder` and how to add providers to the builder is also helpful.

In order to use this Connector you need to do the following:
```
1. Create and bind a Postgres Service instance to your application.
2. Optionally, configure any Postgres client settings (e.g. appsettings.json)
3. Add SteelToe CloudFoundry config provider to your ConfigurationBuilder.
4. Add NpgsqlConnection or DbContext to your ServiceCollection.
```
## Create & Bind Postgres Service
You can create and bind Postgres service instances using the CloudFoundry command line (i.e. cf):
```
1. cf target -o myorg -s myspace
2. cf create-service EDB-Shared-PostgreSQL "Basic PostgreSQL Plan" myPostgres
3. cf bind-service myApp myPostgres
4. cf restage myApp
```
Once you have bound the service to the app, the providers settings have been setup in `VCAP_SERVICES` and will be picked up automatically when the app is started by using the `CloudFoundry` configuration provider at startup.

## Optionally - Configure Postgres Client Settings
Optionally you can configure the settings the Connector will use when setting up the NpgsqlConnection. Typically you would put these in your `appsettings.json` file and use the JSON configuration provider to add them to the applications configuration. Then when the Postgres Connector configures the NpgsqlConnection it will the combine the settings from `appsettings.json` with the settings it obtains from the CloudFoundry configuration provider, with the CloudFoundry settings overriding any settings found in `appsettings.json`.

```
{
...
  "postgres": {
    "client": {
      "host": "myserver",
      "port": 5432
    }
  }
  .....
}
```

 
For a complete list of client settings see the documentation in the `PostgresProviderConnectorOptions` file.

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

## Add NpgsqlConnector or a DbContext
The next step is to add NpgsqlConnector or DbContext's to your ServiceCollection depending on your needs.  You do this in `ConfigureServices(..)` method of the startup class:
```
#using SteelToe.CloudFoundry.Connector.PostgreSql;
... OR
#using SteelToe.CloudFoundry.Connector.PostgreSql.EFCore;

public class Startup {
    .....
    public IConfigurationRoot Configuration { get; private set; }
    public Startup(...)
    {
      .....
    }
    public void ConfigureServices(IServiceCollection services)
    {
        // Add NpgsqlConnector configured from CloudFoundry
        services.AddPostgresConnection(Configuration);

        // OR 

        // If using EFCore
          services.AddDbContext<TestContext>(options => options.UseNpgsql(Configuration));

        // Add framework services.
        services.AddMvc();
        ...
    }
    ....
```
## Using Postres
Below is an example illustrating how to use the DI services to inject a `NpgsqlConnector` or a `DbContext` into a controller:


```
using Npgsql;
....
public class HomeController : Controller
{
    public HomeController()
    {
    }
    ...
    public IActionResult PostgresData(
        [FromServices] NpgsqlConnection dbConnection)
    {
        dbConnection.Open();

        NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM TestData;", dbConnection);
        var rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            ViewData["Key" + rdr[0]] = rdr[1];
        }

        rdr.Close();
        dbConnection.Close();

        return View();
    }
}

-------------------------------------
using Microsoft.EntityFrameworkCore;
...

public class TestContext : DbContext
{
    public TestContext(DbContextOptions options) : base(options)
    {
    }
    public DbSet<TestData> TestData { get; set; }
}

using Project.Models;
....
public class HomeController : Controller
{
    public HomeController()
    {
    }
    public IActionResult PostgresData(
            [FromServices] TestContext context)
    {

        var td = context.TestData.ToList();
        foreach (var d in td)
        {
            ViewData["Key" + d.Id] = d.Data;
        }

        return View();
    }
}
``` 
