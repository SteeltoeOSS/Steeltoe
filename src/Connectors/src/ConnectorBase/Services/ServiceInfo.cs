// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.App;
using System;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public abstract class ServiceInfo : IServiceInfo
    {
        public ServiceInfo(string id)
            : this(id, null)
        {
        }

        public ServiceInfo(string id, ApplicationInstanceInfo info)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            ApplicationInfo = info;
        }

        public string Id { get; internal protected set; }

        public IApplicationInstanceInfo ApplicationInfo { get; internal protected set; }
    }
}
