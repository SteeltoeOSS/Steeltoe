﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public class ExchangeBuilder : AbstractBuilder
    {
        private string _name;
        private string _type;
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
            return new ExchangeBuilder(name, ExchangeType.DIRECT);
        }

        public static ExchangeBuilder TopicExchange(string name)
        {
            return new ExchangeBuilder(name, ExchangeType.TOPIC);
        }

        public static ExchangeBuilder FanoutExchange(string name)
        {
            return new ExchangeBuilder(name, ExchangeType.FANOUT);
        }

        public static ExchangeBuilder HeadersExchange(string name)
        {
            return new ExchangeBuilder(name, ExchangeType.HEADERS);
        }

        public static IExchange Create(string exchangeName, string exchangeType)
        {
            if (ExchangeType.DIRECT.Equals(exchangeType, StringComparison.OrdinalIgnoreCase))
            {
                return new DirectExchange(exchangeName);
            }
            else if (ExchangeType.TOPIC.Equals(exchangeType, StringComparison.OrdinalIgnoreCase))
            {
                return new TopicExchange(exchangeName);
            }
            else if (ExchangeType.FANOUT.Equals(exchangeType, StringComparison.OrdinalIgnoreCase))
            {
                return new FanoutExchange(exchangeName);
            }
            else if (ExchangeType.HEADERS.Equals(exchangeType, StringComparison.OrdinalIgnoreCase))
            {
                return new HeadersExchange(exchangeName);
            }
            else
            {
                return new CustomExchange(exchangeName, exchangeType);
            }
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
            var args = GetOrCreateArguments();
            foreach (var arg in arguments)
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

            foreach (var a in admins)
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
            AbstractExchange exchange = Create(_name, _type) as AbstractExchange;
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
}
