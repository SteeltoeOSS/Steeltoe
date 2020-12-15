using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;
namespace Steeltoe.Stream.Binder.Rabbit
{
    internal class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly ILoggerFactory _factory;

        public XunitLogger(ITestOutputHelper output)
        {
            _output = output;
            _factory = LoggerFactory.Create(builder => builder.AddConsole());
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _output.WriteLine(formatter(state, exception));
            

        }
    }
}
