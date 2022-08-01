// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Messaging.Support;

public abstract class AbstractHeaderMapper<T> : IHeaderMapper<T>
{
    protected readonly ILogger Logger;

    protected AbstractHeaderMapper(ILogger logger = null)
    {
        Logger = logger;
    }

    private string _inboundPrefix = string.Empty;

    private string _outboundPrefix = string.Empty;

    public string InboundPrefix
    {
        get
        {
            return _inboundPrefix;
        }

        set
        {
            _inboundPrefix = value ?? string.Empty;
        }
    }

    public string OutboundPrefix
    {
        get
        {
            return _outboundPrefix;
        }

        set
        {
            _outboundPrefix = value ?? string.Empty;
        }
    }

    public abstract void FromHeaders(IMessageHeaders headers, T target);

    public abstract IMessageHeaders ToHeaders(T source);

    protected virtual string FromHeaderName(string headerName)
    {
        var propertyName = headerName;
        if (!string.IsNullOrEmpty(_outboundPrefix) && !propertyName.StartsWith(_outboundPrefix))
        {
            propertyName = _outboundPrefix + headerName;
        }

        return propertyName;
    }

    protected virtual string ToHeaderName(string propertyName)
    {
        var headerName = propertyName;
        if (!string.IsNullOrEmpty(_inboundPrefix) && !headerName.StartsWith(_inboundPrefix))
        {
            headerName = _inboundPrefix + propertyName;
        }

        return headerName;
    }

    protected virtual TValue GetHeaderIfAvailable<TValue>(IDictionary<string, object> headers, string name)
    {
        headers.TryGetValue(name, out var value);
        if (value == null)
        {
            return default;
        }

        var type = typeof(TValue);

        if (!type.IsInstanceOfType(value))
        {
            Logger?.LogDebug("Skipping header '{headerName}': expected type [{type}], but got [{valueType}]", name, type, value.GetType());
            return default;
        }
        else
        {
            return (TValue)value;
        }
    }
}
