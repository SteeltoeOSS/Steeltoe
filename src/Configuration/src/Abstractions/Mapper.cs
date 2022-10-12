// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;
internal class Mapper
{
    private readonly IDictionary<string, string> _configData;
    private readonly string _bindingKey;
    private readonly string _toPrefix;

    public Mapper(IDictionary<string, string> configData, string bindingKey, params string[] toPrefix)
    {
        _configData = configData;
        _bindingKey = bindingKey + ConfigurationPath.KeyDelimiter;
        if (toPrefix.Length > 0)
        {
            _toPrefix = string.Join(ConfigurationPath.KeyDelimiter, toPrefix) + ConfigurationPath.KeyDelimiter;
        }
    }

    public void MapFromTo(string existingKey, string newKey)
    {
        if (_configData.TryGetValue(_bindingKey + existingKey, out string value))
        {
            if (_toPrefix != null)
            {
                _configData[_toPrefix + newKey] = value;
            }
            else
            {
                _configData[newKey] = value;
            }
        }
    }

    public void MapFromTo(string existingKey, params string[] newKeyPath)
    {
        if (_configData.TryGetValue(_bindingKey + existingKey, out string value))
        {
            var newKey = string.Join(ConfigurationPath.KeyDelimiter, newKeyPath);
            if (_toPrefix != null)
            {
                _configData[_toPrefix + newKey] = value;
            }
            else
            {
                _configData[newKey] = value;
            }
        }
    }
}
