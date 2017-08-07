using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Steeltoe.Extensions.Logging.CloudFoundry
{ 
    public interface ICloudFoundryLoggerSettings : IConsoleLoggerSettings
    {
        void SetLogLevel(string category, LogLevel level);
    }
}
