// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder.Rabbit
{
    internal class XunitLoggerFactory : ILoggerFactory
    {
        private readonly ITestOutputHelper _output;
        private readonly ILoggerFactory _factory;

        public XunitLoggerFactory(ITestOutputHelper output)
        {
            _output = output;
            _factory = LoggerFactory.Create(builder =>
            {
                builder.AddDebug();
            });
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XunitLogger(_output);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
