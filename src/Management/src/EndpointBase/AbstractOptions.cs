// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System;

namespace Steeltoe.Management.Endpoint
{
    [Obsolete("Use AbstractEndpointOptions instead.")]
    public abstract class AbstractOptions : IEndpointOptions
    {
        protected bool? _enabled;

        protected bool? _sensitive;

        public AbstractOptions()
        {
            Global = ManagementOptions.GetInstance();
            Global.EndpointOptions.Add(this);
        }

        public AbstractOptions(string sectionName, IConfiguration config)
        {
            if (sectionName == null)
            {
                throw new ArgumentNullException(nameof(sectionName));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Global = ManagementOptions.GetInstance(config);
            Global.EndpointOptions.Add(this);

            var section = config.GetSection(sectionName);
            if (section != null)
            {
                section.Bind(this);
            }
        }

        public virtual bool IsEnabled
        {
            get
            {
                return Enabled.Value;
            }
        }

        public virtual bool? Enabled
        {
            get
            {
                if (_enabled.HasValue)
                {
                    return _enabled.Value;
                }
                else if (Global.Enabled.HasValue)
                {
                    return Global.Enabled;
                }
                else
                {
                    return DefaultEnabled;
                }
            }

            set
            {
                _enabled = value;
            }
        }

        public virtual bool IsSensitive
        {
            get
            {
                return Sensitive.Value;
            }
        }

        [Obsolete("Use Exposure Options instead")]
        public virtual bool? Sensitive
        {
            get
            {
                if (_sensitive.HasValue)
                {
                    return _sensitive.Value;
                }
                else if (Global.Sensitive.HasValue)
                {
                    return Global.Sensitive;
                }
                else
                {
                    return DefaultSensitive;
                }
            }

            set
            {
                _sensitive = value;
            }
        }

        public virtual IManagementOptions Global { get; set; }

        public virtual string Id { get; set; }

        public virtual string Path
        {
            get
            {
                string path = Global.Path;
                if (string.IsNullOrEmpty(Id))
                {
                    return path;
                }

                if (!path.EndsWith("/"))
                {
                    path = path + "/";
                }

                return path + Id;
            }
        }

        public Permissions RequiredPermissions { get; set; } = Permissions.UNDEFINED;

        public virtual bool IsAccessAllowed(Permissions permissions)
        {
            return permissions >= RequiredPermissions;
        }

        protected virtual bool DefaultEnabled { get; } = true;

        protected virtual bool DefaultSensitive { get; } = true;
    }
}
