// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class ExchangeBuilder : AbstractBuilder
    {
        private string _name;
        private string _type;
        private bool _autoDelete;
        private bool _durable;
        private bool _internal;
        private bool _delayed;
        private bool _ignoreDeclarationExceptions;
        private bool _declare;
        private List<object> _declaringAdmins;

        public ExchangeBuilder(string name, string type)
        {
            _name = name;
            _type = type;
        }

        public static ExchangeBuilder DirectExchange(string name)
        {
            return new ExchangeBuilder(name, ExchangeTypes.DIRECT);
        }

        public static ExchangeBuilder TopicExchange(string name)
        {
            return new ExchangeBuilder(name, ExchangeTypes.TOPIC);
        }

        public static ExchangeBuilder FanoutExchange(string name)
        {
            return new ExchangeBuilder(name, ExchangeTypes.FANOUT);
        }

        public static ExchangeBuilder HeadersExchange(string name)
        {
            return new ExchangeBuilder(name, ExchangeTypes.HEADERS);
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
            AbstractExchange exchange;
            if (ExchangeTypes.DIRECT.Equals(_type))
            {
                exchange = new DirectExchange(_name, _durable, _autoDelete, Arguments);
            }
            else if (ExchangeTypes.TOPIC.Equals(_type))
            {
                exchange = new TopicExchange(_name, _durable, _autoDelete, Arguments);
            }
            else if (ExchangeTypes.FANOUT.Equals(_type))
            {
                exchange = new FanoutExchange(_name, _durable, _autoDelete, Arguments);
            }
            else if (ExchangeTypes.HEADERS.Equals(_type))
            {
                exchange = new HeadersExchange(_name, _durable, _autoDelete, Arguments);
            }
            else
            {
                exchange = new CustomExchange(_name, _type, _durable, _autoDelete, Arguments);
            }

            exchange.IsInternal = _internal;
            exchange.IsDelayed = _delayed;
            exchange.IgnoreDeclarationExceptions = _ignoreDeclarationExceptions;
            exchange.Declare = _declare;
            if (_declaringAdmins != null && _declaringAdmins.Count > 0)
            {
                exchange.SetAdminsThatShouldDeclare(_declaringAdmins.ToArray());
            }

            return exchange;
        }
    }
}
