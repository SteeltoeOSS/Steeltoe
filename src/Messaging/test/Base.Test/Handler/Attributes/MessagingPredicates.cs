// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Test
{
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
}
