using Microsoft.Extensions.Logging;

using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging.CloudFoundry
{ 
    public interface ICloudFoundryLoggerProvider : ILoggerProvider
    {
        ICollection<ILoggerConfiguration> GetLoggerConfigurations();
        void SetLogLevel(string category, LogLevel level);
    }
}
