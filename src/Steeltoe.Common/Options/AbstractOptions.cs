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
using System;

namespace Steeltoe.Common.Options
{
    public abstract class AbstractOptions
    {
        public AbstractOptions()
        {
        }

        public AbstractOptions(IConfigurationRoot root, string sectionPrefix = null)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (!string.IsNullOrEmpty(sectionPrefix))
            {
                var section = root.GetSection(sectionPrefix);
                section.Bind(this);
            }
            else
            {
                root.Bind(this);
            }
        }

        public AbstractOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Bind(this);
        }
    }
}
