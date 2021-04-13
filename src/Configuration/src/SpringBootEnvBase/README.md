# Configuration SpringBoot Env .NET Configuration Provider

This project contains a SpringBoot Env configuration provider.
For more information on how to use this component see the online [Steeltoe documentation](https://steeltoe.io/).

```

# Program.cs

using Steeltoe.Extensions.Configuration.SpringBootEnv;

... 
    class Program
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                        .ConfigureAppConfiguration(conf => conf.AddSpringBootEnvSource())
                        .Build();
            var config = host.Services.GetService(typeof(IConfiguration)) as IConfiguration;
            
            Console.WriteLine(config.GetValue<string>("spring:cloud:stream:input:binding"));
            
            host.Run();
        }
    }

# Windows Command
c:\projects\sample> set SPRING_APPLICATION_JSON={"spring.cloud.stream.input.binding":"barfoo"}
c:\projects\sample> dotnet run
barfoo
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```