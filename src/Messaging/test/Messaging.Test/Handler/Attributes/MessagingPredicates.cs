// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Test;

internal sealed class MessagingPredicates
{
    public static DestinationVariablePredicate DestinationVar()
    {
        return new DestinationVariablePredicate();
    }

    public static DestinationVariablePredicate DestinationVar(string value)
    {
        return new DestinationVariablePredicate().Name(value);
    }

    public static HeaderPredicate Header()
    {
        return new HeaderPredicate();
    }

    public static HeaderPredicate Header(string name)
    {
        return new HeaderPredicate().Name(name);
    }

    public static HeaderPredicate Header(string name, string defaultValue)
    {
        return new HeaderPredicate().Name(name).DefaultValue(defaultValue);
    }

    public static HeaderPredicate HeaderPlain()
    {
        return new HeaderPredicate().NoAttributes();
    }

    internal interface IPredicate<T>
    {
        bool Test(T t);
    }

    internal sealed class DestinationVariablePredicate : IPredicate<ParameterInfo>
    {
        private string _value;

        public DestinationVariablePredicate Name(string name)
        {
            _value = name;
            return this;
        }

        public DestinationVariablePredicate NoName()
        {
            _value = string.Empty;
            return this;
        }

        public bool Test(ParameterInfo parameter)
        {
            var annotation = parameter.GetCustomAttribute<DestinationVariableAttribute>();
            return annotation != null && (_value == null || annotation.Name.Equals(_value));
        }
    }

    internal sealed class HeaderPredicate : IPredicate<ParameterInfo>
    {
        private string _name = string.Empty;
        private bool _required = true;
        private string _defaultValue;

        public HeaderPredicate Name(string name)
        {
            _name = name;
            return this;
        }

        public HeaderPredicate NoName()
        {
            _name = string.Empty;
            return this;
        }

        public HeaderPredicate Required(bool required)
        {
            _required = required;
            return this;
        }

        public HeaderPredicate DefaultValue(string value)
        {
            _defaultValue = value;
            return this;
        }

        public HeaderPredicate NoAttributes()
        {
            _name = string.Empty;
            _required = true;
            _defaultValue = null;
            return this;
        }

        public bool Test(ParameterInfo parameter)
        {
            var annotation = parameter.GetCustomAttribute<HeaderAttribute>();
            return annotation != null && _name == annotation.Name && annotation.Required == _required && _defaultValue == annotation.DefaultValue;
        }
    }
}
