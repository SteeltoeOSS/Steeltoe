// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration
{
    public class KubernetesServicesOptions : ServicesOptions
    {
        public static string ServicesConfigRoot => "helm ????";

        public override string CONFIGURATION_PREFIX { get; protected set; } = ServicesConfigRoot;

        // This constructor is for use with IOptions
        public KubernetesServicesOptions()
        {
        }

        public KubernetesServicesOptions(IConfigurationRoot root)
            : base(root, ServicesConfigRoot)
        {
        }

        public KubernetesServicesOptions(IConfiguration config)
            : base(config, ServicesConfigRoot)
        {
        }
    }
}
