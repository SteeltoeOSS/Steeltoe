// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Info
{
    public class InfoBuilder : IInfoBuilder
    {
        private readonly Dictionary<string, object> _info = new Dictionary<string, object>();

        public Dictionary<string, object> Build()
        {
            return _info;
        }

        public IInfoBuilder WithInfo(string key, object value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _info[key] = value;
            }

            return this;
        }

        public IInfoBuilder WithInfo(Dictionary<string, object> items)
        {
            if (items != null)
            {
                foreach (var pair in items)
                {
                    _info[pair.Key] = pair.Value;
                }
            }

            return this;
        }
    }
}
