// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.RabbitMQ.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public class DeclareQueueBindingAttribute : Attribute
    {
        private string _bindingName;

        public string Name
        {
            get
            {
                if (_bindingName == null)
                {
                    return ExchangeName + "." + QueueName;
                }

                return _bindingName;
            }

            set
            {
                _bindingName = value;
            }
        }

        public string QueueName { get; set; } = string.Empty;

        public string ExchangeName { get; set; } = string.Empty;

        public string RoutingKey
        {
            get
            {
                if (RoutingKeys.Length == 0)
                {
                    return null;
                }

                return RoutingKeys[0];
            }

            set
            {
                RoutingKeys = new string[] { value };
            }
        }

        public string[] RoutingKeys { get; set; } = Array.Empty<string>();

        public string IgnoreDeclarationExceptions { get; set; } = "False";

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
}
