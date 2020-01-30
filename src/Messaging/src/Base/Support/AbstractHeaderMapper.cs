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

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Support
{
    public abstract class AbstractHeaderMapper<T> : IHeaderMapper<T>
    {
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

        protected virtual V GetHeaderIfAvailable<V>(IDictionary<string, object> headers, string name)
        {
            headers.TryGetValue(name, out var value);
            if (value == null)
            {
                return default;
            }

            var type = typeof(V);

            if (!type.IsAssignableFrom(value.GetType()))
            {
                // if (logger.isDebugEnabled())
                // {
                //    logger.debug("Skipping header '" + name + "': expected type [" + type + "], but got [" +
                //            value.getClass() + "]");
                // }
                return default;
            }
            else
            {
                return (V)value;
            }
        }
    }
}
