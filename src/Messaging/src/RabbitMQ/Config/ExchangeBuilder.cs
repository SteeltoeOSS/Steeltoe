// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class ExchangeBuilder : AbstractBuilder
{
    private readonly string _name;
    private readonly string _type;
    private bool _autoDelete;
    private bool _durable = true;
    private bool _internal;
    private bool _delayed;
    private bool _ignoreDeclarationExceptions;
    private bool _declare = true;
    private List<object> _declaringAdmins;

    public ExchangeBuilder(string name, string type)
    {
        _name = name;
        _type = type;
    }

    public static ExchangeBuilder DirectExchange(string name)
    {
        return new ExchangeBuilder(name, ExchangeType.Direct);
    }

    public static ExchangeBuilder TopicExchange(string name)
    {
        return new ExchangeBuilder(name, ExchangeType.Topic);
    }

    public static ExchangeBuilder FanOutExchange(string name)
    {
        return new ExchangeBuilder(name, ExchangeType.FanOut);
    }

    public static ExchangeBuilder HeadersExchange(string name)
    {
        return new ExchangeBuilder(name, ExchangeType.Headers);
    }

    public static IExchange Create(string exchangeName, string exchangeType)
    {
        if (ExchangeType.Direct.Equals(exchangeType, StringComparison.OrdinalIgnoreCase))
        {
            return new DirectExchange(exchangeName);
        }

        if (ExchangeType.Topic.Equals(exchangeType, StringComparison.OrdinalIgnoreCase))
        {
            return new TopicExchange(exchangeName);
        }

        if (ExchangeType.FanOut.Equals(exchangeType, StringComparison.OrdinalIgnoreCase))
        {
            return new FanOutExchange(exchangeName);
        }

        if (ExchangeType.Headers.Equals(exchangeType, StringComparison.OrdinalIgnoreCase))
        {
            return new HeadersExchange(exchangeName);
        }

        return new CustomExchange(exchangeName, exchangeType);
    }

    public ExchangeBuilder AutoDelete()
    {
        _autoDelete = true;
        return this;
    }

    public ExchangeBuilder Durable(bool isDurable)
    {
        _durable = isDurable;
        return this;
    }

    public ExchangeBuilder WithArgument(string key, object value)
    {
        GetOrCreateArguments().Add(key, value);
        return this;
    }

    public ExchangeBuilder WithArguments(Dictionary<string, object> arguments)
    {
        Dictionary<string, object> args = GetOrCreateArguments();

        foreach (KeyValuePair<string, object> arg in arguments)
        {
            args.Add(arg.Key, arg.Value);
        }

        return this;
    }

    public ExchangeBuilder Alternate(string exchange)
    {
        return WithArgument("alternate-exchange", exchange);
    }

    public ExchangeBuilder Internal()
    {
        _internal = true;
        return this;
    }

    public ExchangeBuilder Delayed()
    {
        _delayed = true;
        return this;
    }

    public ExchangeBuilder IgnoreDeclarationExceptions()
    {
        _ignoreDeclarationExceptions = true;
        return this;
    }

    public ExchangeBuilder SuppressDeclaration()
    {
        _declare = false;
        return this;
    }

    public ExchangeBuilder Admins(params object[] admins)
    {
        if (admins == null)
        {
            throw new ArgumentNullException(nameof(admins));
        }

        foreach (object a in admins)
        {
            if (a == null)
            {
                throw new ArgumentNullException("'admins' can't have null elements");
            }
        }

        _declaringAdmins = new List<object>(admins);
        return this;
    }

    public AbstractExchange Build()
    {
        var exchange = Create(_name, _type) as AbstractExchange;
        exchange.IsDurable = _durable;
        exchange.IsAutoDelete = _autoDelete;
        exchange.Arguments = Arguments;
        exchange.IsInternal = _internal;
        exchange.IsDelayed = _delayed;
        exchange.IgnoreDeclarationExceptions = _ignoreDeclarationExceptions;
        exchange.ShouldDeclare = _declare;

        if (_declaringAdmins != null && _declaringAdmins.Count > 0)
        {
            exchange.SetAdminsThatShouldDeclare(_declaringAdmins.ToArray());
        }

        return exchange;
    }
}
