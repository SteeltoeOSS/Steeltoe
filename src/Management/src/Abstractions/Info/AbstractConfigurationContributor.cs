﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Info
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
            if (_config != null)
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                Dictionary<string, object> dict = result;

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
