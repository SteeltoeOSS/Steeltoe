# Configuration SpringBoot Env .NET Configuration Provider

This project contains configuration providers for environments friendly to Spring Boot applications like SCDF. The configuration may be provided as a json string inside a single environment variable that looks like 
`{"spring.cloud.stream.input.binding":"barfoo"}` or as a command-line parameter that looks like `spring.cloud.stream.input.binding=barfoo`.
For more information on how to use this component see the online [Steeltoe documentation](https://steeltoe.io/).

```
// Program.cs

using Steeltoe.Extensions.Configuration.SpringBoot;

... 
internal static class Program
{
    private static void Main(string[] args)
    {
        IHost host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
            {
                configurationBuilder.AddSpringBootEnv(); // Can be used together or independently
                configurationBuilder.AddSpringBootCmd(hostBuilderContext.Configuration);
            })
            .Build();

        var configuration = (IConfiguration)host.Services.GetService(typeof(IConfiguration));

        Console.WriteLine(configuration.GetValue<string>("spring:cloud:stream:input:binding"));

        host.Run();
    }
}
```

# Windows command 
## Using SPRING_APPLICATION_JSON environment variable

```
c:\projects\sample> set SPRING_APPLICATION_JSON={"spring.cloud.stream.input.binding":"barfoo"}
c:\projects\sample> dotnet run
barfoo
info: Microsoft.Hosting.Lifetime[0]
...
```

## Using command-line arguments

```
c:\projects\sample> dotnet run -- spring.cloud.stream.input.binding=barfoo
barfoo
info: Microsoft.Hosting.Lifetime[0]
...
```
