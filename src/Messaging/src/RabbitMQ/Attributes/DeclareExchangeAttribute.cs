// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Config;
using System;

namespace Steeltoe.Messaging.RabbitMQ.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public class DeclareExchangeAttribute : Attribute
{
    public DeclareExchangeAttribute()
    {
    }

    public string Name { get; set; }

    public string Type { get; set; } = ExchangeType.DIRECT;

    public string Durable { get; set; } = "True";

    public string AutoDelete { get; set; } = "False";

    public string Internal { get; set; } = "False";

    public string IgnoreDeclarationExceptions { get; set; } = "False";

    public string Delayed { get; set; } = "False";

    public string Declare { get; set; } = "True";

    public string Admin
    {
        get
        {
            if (Admins.Length == 0)
            {
                return null;
            }

            return Admins[0];
        }

        set
        {
            Admins = new string[] { value };
        }
    }

    public string[] Admins { get; set; } = Array.Empty<string>();
}