# Configuration SpringBoot Env .NET Configuration Provider

This project contains configuration providers for environments friendly to Spring Boot applications like SCDF. The configuration may be provided as a json string inside a single env variable that looks like 
`{"spring.cloud.stream.input.binding":"barfoo"}` or as a Command line parameter that looks like `spring.cloud.stream.input.binding=barfoo`.
For more information on how to use this component see the online [Steeltoe documentation](https://steeltoe.io/).

```

# Program.cs

using Steeltoe.Extensions.Configuration.SpringBoot;

... 
    class Program
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                        .ConfigureAppConfiguration((ctx, conf) => {
                                            conf.AddSpringBootEnv(); // Can be used together or independently
                                            conf.AddSpringBootCmd(ctx.Configuration);
                                            })
                        .Build();

            var config = host.Services.GetService(typeof(IConfiguration)) as IConfiguration;
            
            Console.WriteLine(config.GetValue<string>("spring:cloud:stream:input:binding"));
            
            host.Run();
        }
    }

# Windows Command 
## Using SPRING_APPLICATION_JSON

c:\projects\sample> set SPRING_APPLICATION_JSON={"spring.cloud.stream.input.binding":"barfoo"}
c:\projects\sample> dotnet run
barfoo
info: Microsoft.Hosting.Lifetime[0]
...

## Using Command Line Args

c:\projects\sample> dotnet run -- spring.cloud.stream.input.binding=barfoo
barfoo
info: Microsoft.Hosting.Lifetime[0]
...
```

