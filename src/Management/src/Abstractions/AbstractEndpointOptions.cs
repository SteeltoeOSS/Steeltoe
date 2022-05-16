// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management
{
    public abstract class AbstractEndpointOptions : IEndpointOptions
    {
        protected bool? _enabled;

        protected bool? _sensitive;

        protected string _path;

        protected AbstractEndpointOptions()
        {
        }

        protected AbstractEndpointOptions(string sectionName, IConfiguration config)
        {
            if (sectionName == null)
            {
                throw new ArgumentNullException(nameof(sectionName));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(sectionName);
            if (section != null)
            {
                section.Bind(this);
            }

            // These should not be set from configuration
            AllowedVerbs = null;
            ExactMatch = true;
        }

        public virtual bool? Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                _enabled = value;
            }
        }

        public virtual string Id { get; set; }

        public virtual string Path
        {
            get
            {
                if (!string.IsNullOrEmpty(_path))
                {
                    return _path;
                }

                return Id;
            }

            set
            {
                _path = value;
            }
        }

        public Permissions RequiredPermissions { get; set; } = Permissions.UNDEFINED;

        public IManagementOptions Global { get; set; }

        public virtual bool IsAccessAllowed(Permissions permissions)
        {
            return permissions >= RequiredPermissions;
        }

        public virtual bool DefaultEnabled { get; } = true;

        public IEnumerable<string> AllowedVerbs { get; set; }

        public bool ExactMatch { get; set; } = true;
    }
}
