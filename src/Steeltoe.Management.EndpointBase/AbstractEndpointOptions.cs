// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System;

namespace Steeltoe.Management.Endpoint
{
    public abstract class AbstractEndpointOptions : IEndpointOptions
    {
        protected bool? _enabled;

        protected bool? _sensitive;

        protected string _path;

        public AbstractEndpointOptions()
        {
        }

        public AbstractEndpointOptions(string sectionName, IConfiguration config)
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
        }

        [Obsolete]
        public virtual bool IsEnabled
        {
            get
            {
                return false;
            }
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

        [Obsolete]
        public virtual bool IsSensitive
        {
            get { return false; }
        }

        public virtual bool? Sensitive
        {
            get
            {
                return _sensitive;
            }

            set
            {
                _sensitive = value;
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

        public virtual bool DefaultSensitive { get; } = true;
    }
}
