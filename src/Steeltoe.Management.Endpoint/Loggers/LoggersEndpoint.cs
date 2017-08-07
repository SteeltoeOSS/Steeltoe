using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Logging.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggersEndpoint : AbstractEndpoint<Dictionary<string, object>, LoggersChangeRequest>
    {
        ILogger<LoggersEndpoint> _logger;
        protected new ILoggersOptions Options
        {
            get
            {
                return options as ILoggersOptions;
            }
        }

        public LoggersEndpoint(ILoggersOptions options, ILogger<LoggersEndpoint> logger) : base(options)
        {
            _logger = logger;
        }

        public override Dictionary<string, object> Invoke(LoggersChangeRequest request)
        {
            _logger.LogDebug("Invoke({0})", request);

            var provider = CloudFoundryLoggerProvider.Instance;
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (provider == null)
            {
                return result;
            }

            AddLevels(result);

            var configuration = provider.GetLoggerConfigurations();
            if (request != null)
            {

                provider.SetLogLevel(request.Name, LoggerLevels.MapLogLevel(request.Level));

            } else
            {
                Dictionary<string, LoggerLevels> loggers = new Dictionary<string, LoggerLevels>();
                foreach(var c in configuration)
                {
                    _logger.LogTrace("Adding " + c.ToString());
                    LoggerLevels lv = new LoggerLevels(c.ConfiguredLevel, c.EffectiveLevel);
                    loggers.Add(c.Name, lv);
                }
                result.Add("loggers", loggers);
            }

            return result;

        }

        private void AddLevels(Dictionary<string, object> result)
        {
            result.Add("levels", levels);
        }
        private static List<string> levels = new List<string>() {
            LoggerLevels.MapLogLevel(LogLevel.None),
            LoggerLevels.MapLogLevel(LogLevel.Critical),
            LoggerLevels.MapLogLevel(LogLevel.Error),
            LoggerLevels.MapLogLevel(LogLevel.Warning),
            LoggerLevels.MapLogLevel(LogLevel.Information),
            LoggerLevels.MapLogLevel(LogLevel.Debug),
            LoggerLevels.MapLogLevel(LogLevel.Trace)
        };
    }
}
