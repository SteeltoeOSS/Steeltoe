using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint
{
    public abstract class AbstractOptions : IEndpointOptions
    {
        private bool? _enabled;
        public virtual bool Enabled
        {
            get
            {
                if (_enabled.HasValue) return _enabled.Value; else return Global.Enabled;
            }
            set
            {
                _enabled = value;
            }
        }
        private bool? _sensitive;
        public virtual bool Sensitive
        {
            get
            {
                if (_sensitive.HasValue) return _sensitive.Value; else return Global.Sensitive;
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
                    return path;

                if (!path.EndsWith("/"))
                {
                    path = path + "/";
                }
                return path + Id;
            }
        }

        public Permissions RequiredPermissions { get; set; } = Permissions.UNDEFINED;

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

        public virtual bool IsAccessAllowed(Permissions permissions)
        {
            return permissions >= RequiredPermissions;
        }
    }
}
