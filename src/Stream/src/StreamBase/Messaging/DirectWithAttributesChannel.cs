// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using System.Collections.Generic;

namespace Steeltoe.Stream.Messaging;

public class DirectWithAttributesChannel : DirectChannel
{
    private readonly IDictionary<string, object> _attributes = new Dictionary<string, object>();

    public DirectWithAttributesChannel(IApplicationContext context)
        : base(context)
    {
    }

    public void SetAttribute(string key, object value)
    {
        _attributes[key] = value;
    }

    public object GetAttribute(string key)
    {
        return _attributes[key];
    }
}
