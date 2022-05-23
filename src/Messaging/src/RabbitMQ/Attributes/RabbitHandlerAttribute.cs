// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.RabbitMQ.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RabbitHandlerAttribute : Attribute
    {
        public RabbitHandlerAttribute(bool isDefault = false)
        {
            IsDefault = isDefault;
        }

        public bool IsDefault { get; set; }
    }
}
