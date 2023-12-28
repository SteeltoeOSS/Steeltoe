using Microsoft.Data.SqlClient;
using Steeltoe.Connector.SqlServer.EFCore;

namespace Steeltoe.Connector.Example;

public class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                var conn = new SqlConnectionStringBuilder()
                {
                    DataSource = "localhost\\SQLExpress2019",
                    UserID = "steeltoe",
                    Password = "steeltoe",
                    InitialCatalog = "mydb",
                    TrustServerCertificate = true,
                    MaxPoolSize = 50,
                };
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["sqlserver:credentials:ConnectionString"] = conn.ConnectionString,
                });
            })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddDbContext<GoodDbContext>(options =>
                {
                    options.UseSqlServer(hostContext.Configuration);
                    //options.UseSqlServer(ConnectionStringHelper.ConnectionString);
                });
                services.AddSingleton<ICMSComm, CMSComm>();
                services.AddControllersWithViews();
                services.AddHostedService<Worker>();
            })
            .UseWindowsService();

}
