// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Logging.DynamicSerilog;

public class SerilogDynamicLoggerFactory : ILoggerFactory
{
    private readonly IDynamicLoggerProvider _provider;

    public SerilogDynamicLoggerFactory(IDynamicLoggerProvider provider)
    {
        _provider = provider;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _provider.CreateLogger(categoryName);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // noop
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _provider?.Dispose();
        }
    }
}
