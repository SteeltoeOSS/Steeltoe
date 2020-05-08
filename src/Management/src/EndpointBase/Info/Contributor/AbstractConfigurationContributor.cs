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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Info.Contributor
{
    public abstract class AbstractConfigurationContributor
    {
        protected IConfiguration _config;

        protected AbstractConfigurationContributor()
        {
        }

        protected AbstractConfigurationContributor(IConfiguration config)
        {
            _config = config;
        }

        protected virtual void Contribute(IInfoBuilder builder, string prefix, bool keepPrefix)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.WithInfo(CreateDictionary(prefix, keepPrefix));
        }

        protected virtual Dictionary<string, object> CreateDictionary(string prefix, bool keepPrefix)
        {
            var result = new Dictionary<string, object>();
            if (_config != null)
            {
                var dict = result;

                var section = _config.GetSection(prefix);
                var children = section.GetChildren();

                if (keepPrefix)
                {
                    result[prefix] = dict = new Dictionary<string, object>();
                }

                foreach (var child in children)
                {
                    AddChildren(dict, children);
                }

                return result;
            }

            return null;
        }

        protected virtual void AddChildren(Dictionary<string, object> dict, IEnumerable<IConfigurationSection> sections)
        {
            foreach (var section in sections)
            {
                var key = section.Key;
                var val = section.Value;
                if (val == null)
                {
                    var newDict = new Dictionary<string, object>();
                    dict[key] = newDict;
                    AddChildren(newDict, section.GetChildren());
                }
                else
                {
                    AddKeyValue(dict, key, val);
                }
            }
        }

        protected virtual void AddKeyValue(Dictionary<string, object> dict, string key, string value)
        {
            dict[key] = value;
        }
    }
}
