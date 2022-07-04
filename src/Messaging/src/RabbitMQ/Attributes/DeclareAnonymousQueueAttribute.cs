// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System;

namespace Steeltoe.Messaging.RabbitMQ.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public class DeclareAnonymousQueueAttribute : DeclareQueueBaseAttribute
{
    public DeclareAnonymousQueueAttribute(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException(nameof(id));
        }

        Id = id;
        Name = Base64UrlNamingStrategy.Default.GenerateName();
    }

    public string Id { get; set; }

    internal string Name { get; }
}
