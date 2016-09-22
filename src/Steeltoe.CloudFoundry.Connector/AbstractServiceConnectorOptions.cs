//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.Extensions.Configuration;
using System;
using System.Text;

namespace Steeltoe.CloudFoundry.Connector
{
    public abstract class AbstractServiceConnectorOptions
    {
        protected AbstractServiceConnectorOptions()
        {

        }
        public AbstractServiceConnectorOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            config.Bind(this);
        }
        internal protected void AddKeyValue(StringBuilder sb, string key, int value)
        {
            AddKeyValue(sb, key, value.ToString());
        }
        internal protected void AddKeyValue(StringBuilder sb, string key, bool value)
        {
            AddKeyValue(sb, key, value.ToString().ToLowerInvariant());
        }
        internal protected void AddKeyValue(StringBuilder sb, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                sb.Append(key);
                sb.Append("=");
                sb.Append(value);
                sb.Append(";");
            }
        }
    }
}
