// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using static Steeltoe.Messaging.RabbitMQ.Config.Binding;

namespace Steeltoe.Messaging.RabbitMQ.Config;

public class BindingBuilder
{
    public static DestinationConfigurer Bind(IQueue queue)
    {
        return new DestinationConfigurer(queue.QueueName, DestinationType.QUEUE);
    }

    public static DestinationConfigurer Bind(IExchange exchange)
    {
        return new DestinationConfigurer(exchange.ExchangeName, DestinationType.EXCHANGE);
    }

    private static Dictionary<string, object> CreateMapForKeys(params string[] keys)
    {
        var map = new Dictionary<string, object>();
        foreach (var key in keys)
        {
            map[key] = null;
        }

        return map;
    }

    public class DestinationConfigurer
    {
        public string Name { get; }

        public DestinationType Type { get; }

        public DestinationConfigurer(string destinationName, DestinationType type)
        {
            Name = destinationName;
            Type = type;
        }

        public IBinding To(FanoutExchange exchange)
        {
            var bindingName = exchange.ExchangeName + "." + Name;
            return Binding.Create(bindingName, Name, Type, exchange.ExchangeName, string.Empty, new Dictionary<string, object>());
        }

        public HeadersExchangeMapConfigurer To(HeadersExchange exchange)
        {
            return new HeadersExchangeMapConfigurer(this, exchange);
        }

        public DirectExchangeRoutingKeyConfigurer To(DirectExchange exchange)
        {
            return new DirectExchangeRoutingKeyConfigurer(this, exchange);
        }

        public TopicExchangeRoutingKeyConfigurer To(TopicExchange exchange)
        {
            return new TopicExchangeRoutingKeyConfigurer(this, exchange);
        }

        public GenericExchangeRoutingKeyConfigurer To(IExchange exchange)
        {
            return new GenericExchangeRoutingKeyConfigurer(this, exchange);
        }
    }

    public class HeadersExchangeMapConfigurer
    {
        public DestinationConfigurer Destination { get; }

        public HeadersExchange Exchange { get; }

        public HeadersExchangeMapConfigurer(DestinationConfigurer destination, HeadersExchange exchange)
        {
            Destination = destination;
            Exchange = exchange;
        }

        public HeadersExchangeSingleValueBindingCreator Where(string key)
        {
            return new HeadersExchangeSingleValueBindingCreator(this, key);
        }

        public HeadersExchangeKeysBindingCreator WhereAny(params string[] headerKeys)
        {
            return new HeadersExchangeKeysBindingCreator(this, headerKeys, false);
        }

        public HeadersExchangeMapBindingCreator WhereAny(Dictionary<string, object> headerValues)
        {
            return new HeadersExchangeMapBindingCreator(this, headerValues, false);
        }

        public HeadersExchangeKeysBindingCreator WhereAll(params string[] headerKeys)
        {
            return new HeadersExchangeKeysBindingCreator(this, headerKeys, true);
        }

        public HeadersExchangeMapBindingCreator WhereAll(Dictionary<string, object> headerValues)
        {
            return new HeadersExchangeMapBindingCreator(this, headerValues, true);
        }

        public class HeadersExchangeSingleValueBindingCreator
        {
            private readonly string _key;
            private readonly HeadersExchangeMapConfigurer _configurer;

            public HeadersExchangeSingleValueBindingCreator(HeadersExchangeMapConfigurer configurer, string key)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                _key = key;
                _configurer = configurer;
            }

            public IBinding Exists()
            {
                var bindingName = _configurer.Exchange.ExchangeName + "." + _configurer.Destination.Name;
                return Binding.Create(
                    bindingName,
                    _configurer.Destination.Name,
                    _configurer.Destination.Type,
                    _configurer.Exchange.ExchangeName,
                    string.Empty,
                    CreateMapForKeys(_key));
            }

            public IBinding Matches(object value)
            {
                var map = new Dictionary<string, object>();
                map[_key] = value;
                var bindingName = _configurer.Exchange.ExchangeName + "." + _configurer.Destination.Name;
                return Binding.Create(
                    bindingName,
                    _configurer.Destination.Name,
                    _configurer.Destination.Type,
                    _configurer.Exchange.ExchangeName,
                    string.Empty,
                    map);
            }
        }

        public class HeadersExchangeKeysBindingCreator
        {
            private readonly Dictionary<string, object> _headerMap;
            private readonly HeadersExchangeMapConfigurer _configurer;

            public HeadersExchangeKeysBindingCreator(HeadersExchangeMapConfigurer configurer, string[] headerKeys, bool matchAll)
            {
                if (headerKeys == null || headerKeys.Length == 0)
                {
                    throw new ArgumentException(nameof(headerKeys));
                }

                _headerMap = CreateMapForKeys(headerKeys);
                _headerMap["x-match"] = matchAll ? "all" : "any";
                _configurer = configurer;
            }

            public IBinding Exist()
            {
                var bindingName = _configurer.Exchange.ExchangeName + "." + _configurer.Destination.Name;
                return Binding.Create(
                    bindingName,
                    _configurer.Destination.Name,
                    _configurer.Destination.Type,
                    _configurer.Exchange.ExchangeName,
                    string.Empty,
                    _headerMap);
            }
        }

        public class HeadersExchangeMapBindingCreator
        {
            private readonly Dictionary<string, object> _headerMap;
            private readonly HeadersExchangeMapConfigurer _configurer;

            public HeadersExchangeMapBindingCreator(HeadersExchangeMapConfigurer configurer, Dictionary<string, object> headerMap, bool matchAll)
            {
                if (headerMap == null || headerMap.Count == 0)
                {
                    throw new ArgumentException(nameof(headerMap));
                }

                _headerMap = new Dictionary<string, object>(headerMap);
                _headerMap["x-match"] = matchAll ? "all" : "any";
                _configurer = configurer;
            }

            public IBinding Match()
            {
                var bindingName = _configurer.Exchange.ExchangeName + "." + _configurer.Destination.Name;
                return Binding.Create(
                    bindingName,
                    _configurer.Destination.Name,
                    _configurer.Destination.Type,
                    _configurer.Exchange.ExchangeName,
                    string.Empty,
                    _headerMap);
            }
        }
    }

    public abstract class AbstractRoutingKeyConfigurer
    {
        public DestinationConfigurer Destination { get; }

        public string ExchangeName { get; }

        protected AbstractRoutingKeyConfigurer(DestinationConfigurer destination, string exchange)
        {
            Destination = destination;
            ExchangeName = exchange;
        }
    }

    public class TopicExchangeRoutingKeyConfigurer : AbstractRoutingKeyConfigurer
    {
        public TopicExchangeRoutingKeyConfigurer(DestinationConfigurer destination, TopicExchange exchange)
            : base(destination, exchange.ExchangeName)
        {
        }

        public IBinding With(string routingKey)
        {
            var bindingName = ExchangeName + "." + Destination.Name;
            return Binding.Create(bindingName, Destination.Name, Destination.Type, ExchangeName, routingKey, new Dictionary<string, object>());
        }

        public IBinding With(Enum routingKeyEnum)
        {
            var bindingName = ExchangeName + "." + Destination.Name;
            return Binding.Create(bindingName, Destination.Name, Destination.Type, ExchangeName, routingKeyEnum.ToString(), new Dictionary<string, object>());
        }
    }

    public class GenericExchangeRoutingKeyConfigurer : AbstractRoutingKeyConfigurer
    {
        public GenericExchangeRoutingKeyConfigurer(DestinationConfigurer destination, IExchange exchange)
            : base(destination, exchange.ExchangeName)
        {
        }

        public GenericArgumentsConfigurer With(string routingKey)
        {
            return new GenericArgumentsConfigurer(this, routingKey);
        }

        public GenericArgumentsConfigurer With(Enum routingKeyEnum)
        {
            return new GenericArgumentsConfigurer(this, routingKeyEnum.ToString());
        }
    }

    public class GenericArgumentsConfigurer
    {
        private readonly GenericExchangeRoutingKeyConfigurer _configurer;

        private readonly string _routingKey;

        public GenericArgumentsConfigurer(GenericExchangeRoutingKeyConfigurer configurer, string routingKey)
        {
            _configurer = configurer;
            _routingKey = routingKey;
        }

        public IBinding And(Dictionary<string, object> map)
        {
            var bindingName = _configurer.ExchangeName + "." + _configurer.Destination.Name;
            return Binding.Create(bindingName, _configurer.Destination.Name, _configurer.Destination.Type, _configurer.ExchangeName, _routingKey, map);
        }

        public IBinding NoArgs()
        {
            var bindingName = _configurer.ExchangeName + "." + _configurer.Destination.Name;
            return Binding.Create(bindingName, _configurer.Destination.Name, _configurer.Destination.Type, _configurer.ExchangeName, _routingKey, new Dictionary<string, object>());
        }
    }

    public class DirectExchangeRoutingKeyConfigurer : AbstractRoutingKeyConfigurer
    {
        public DirectExchangeRoutingKeyConfigurer(DestinationConfigurer destination, DirectExchange exchange)
            : base(destination, exchange.ExchangeName)
        {
        }

        public IBinding With(string routingKey)
        {
            var bindingName = ExchangeName + "." + Destination.Name;
            return Binding.Create(bindingName, Destination.Name, Destination.Type, ExchangeName, routingKey, new Dictionary<string, object>());
        }

        public IBinding With(Enum routingKeyEnum)
        {
            var bindingName = ExchangeName + "." + Destination.Name;
            return Binding.Create(bindingName, Destination.Name, Destination.Type, ExchangeName, routingKeyEnum.ToString(), new Dictionary<string, object>());
        }

        public IBinding WithQueueName()
        {
            var bindingName = ExchangeName + "." + Destination.Name;
            return Binding.Create(bindingName, Destination.Name, Destination.Type, ExchangeName, Destination.Name, new Dictionary<string, object>());
        }
    }

    public static IBinding Create(string bindingName, DestinationType type)
    {
        if (type == DestinationType.EXCHANGE)
        {
            return new ExchangeBinding(bindingName);
        }

        return new QueueBinding(bindingName);
    }
}