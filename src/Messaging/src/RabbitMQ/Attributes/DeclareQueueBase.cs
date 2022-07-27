// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.RabbitMQ.Attributes;

public abstract class DeclareQueueBase : Attribute
{
    public virtual string Durable { get; set; } = string.Empty;

    public virtual string Exclusive { get; set; } = string.Empty;

    public virtual string AutoDelete { get; set; } = string.Empty;

    public virtual string IgnoreDeclarationExceptions { get; set; } = "False";

    public virtual string Declare { get; set; } = "True";

    public virtual string Admin
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

    public virtual string[] Admins { get; set; } = Array.Empty<string>();
}