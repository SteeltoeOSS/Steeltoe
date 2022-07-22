// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Test;

internal class MessagingPredicates
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

    internal class DestinationVariablePredicate : IPredicate<ParameterInfo>
    {
        private string value;

        public DestinationVariablePredicate Name(string name)
        {
            value = name;
            return this;
        }

        public DestinationVariablePredicate NoName()
        {
            value = string.Empty;
            return this;
        }

        public bool Test(ParameterInfo parameter)
        {
            var annotation = parameter.GetCustomAttribute<DestinationVariableAttribute>();
            return annotation != null && (value == null || annotation.Name.Equals(value));
        }
    }

    internal class HeaderPredicate : IPredicate<ParameterInfo>
    {
        private string name = string.Empty;
        private bool required = true;
        private string defaultValue = null;

        public HeaderPredicate Name(string name)
        {
            this.name = name;
            return this;
        }

        public HeaderPredicate NoName()
        {
            name = string.Empty;
            return this;
        }

        public HeaderPredicate Required(bool required)
        {
            this.required = required;
            return this;
        }

        public HeaderPredicate DefaultValue(string value)
        {
            defaultValue = value;
            return this;
        }

        public HeaderPredicate NoAttributes()
        {
            name = string.Empty;
            required = true;
            defaultValue = null;
            return this;
        }

        public bool Test(ParameterInfo parameter)
        {
            var annotation = parameter.GetCustomAttribute<HeaderAttribute>();
            return annotation != null &&
                   name == annotation.Name &&
                   annotation.Required == required &&
                   defaultValue == annotation.DefaultValue;
        }
    }
}