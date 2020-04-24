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
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public class KubernetesApplicationOptions : ApplicationInstanceInfo
    {
        public static string PlatformConfigRoot => "spring:cloud:kubernetes";

        protected override string PlatformRoot => PlatformConfigRoot;

        public KubernetesApplicationOptions()
        {
        }

        public KubernetesApplicationOptions(IConfiguration config)
            : base(config.GetSection(PlatformConfigRoot))
        {
            Name ??= ApplicationNameInContext(SteeltoeComponent.Kubernetes);
            Config ??= new KubernetesConfiguration();
            Secrets ??= new WatchableResource();
            Reload ??= new ReloadSettings();
        }

        public bool Enabled { get; set; } = true;

        public string Name { get; set; }

        public string NameSpace { get; set; } = "default";

        public ReloadSettings Reload { get; set; }

        /// <summary>
        /// List of name/namespace of additional configmaps to retrieve
        /// </summary>
        public KubernetesConfiguration Config { get; set; }

        /// <summary>
        /// List of name/namespace of additional secrets to retrieve
        /// </summary>
        public WatchableResource Secrets { get; set; }

        public string NameEnvironmentSeparator { get; set; } = ".";
    }
}
