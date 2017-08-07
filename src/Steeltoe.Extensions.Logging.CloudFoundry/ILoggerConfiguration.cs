using Microsoft.Extensions.Logging;


namespace Steeltoe.Extensions.Logging.CloudFoundry
{ 
    public interface ILoggerConfiguration
    {
        string Name { get; }
        LogLevel? ConfiguredLevel { get; }
        LogLevel EffectiveLevel { get; }
    }
}
