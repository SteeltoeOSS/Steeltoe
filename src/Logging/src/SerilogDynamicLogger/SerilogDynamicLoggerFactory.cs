using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger
{
    public class SerilogDynamicLoggerFactory : ILoggerFactory
    {
        private readonly IDynamicLoggerProvider _provider;

        public SerilogDynamicLoggerFactory(IDynamicLoggerProvider provider)
        {
            _provider = provider;
        }

        public void Dispose()
        {
            _provider.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _provider.CreateLogger(categoryName);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
}