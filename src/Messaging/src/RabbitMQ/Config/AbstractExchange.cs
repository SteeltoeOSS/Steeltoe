// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public abstract class AbstractExchange : AbstractDeclarable, IExchange
{
    protected AbstractExchange(string exchangeName)
        : this(exchangeName, true, false)
    {
    }

    protected AbstractExchange(string exchangeName, bool durable, bool autoDelete)
        : this(exchangeName, durable, autoDelete, null)
    {
    }

    protected AbstractExchange(string exchangeName, bool durable, bool autoDelete, Dictionary<string, object> arguments)
        : base(arguments)
    {
        ExchangeName = exchangeName;
        ServiceName = !string.IsNullOrEmpty(exchangeName) ? exchangeName : $"exchange@{RuntimeHelpers.GetHashCode(this)}";
        IsDurable = durable;
        IsAutoDelete = autoDelete;
    }

    public string ServiceName { get; set; }

    public string ExchangeName { get; set; }

    public bool IsDurable { get; set; }

    public bool IsAutoDelete { get; set; }

    public bool IsDelayed { get; set; }

    public bool IsInternal { get; set; }

    public abstract string Type { get; }

    public override string ToString()
    {
        return
            $"Exchange [name={ExchangeName}, type={Type}, durable={IsDurable}, autoDelete={IsAutoDelete}, internal={IsInternal}, arguments={Arguments}]";
    }
}
