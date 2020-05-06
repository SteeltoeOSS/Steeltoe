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

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Kubernetes;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    internal class KubernetesConfigSourceSettings
    {
        internal KubernetesConfigSourceSettings(string @namespace, string name, ReloadSettings reloadSettings, ILogger logger = null)
        {
            Namespace = @namespace ?? "default";
            Name = name;
            ReloadSettings = reloadSettings;
            Logger = logger;
        }

        internal string Name { get; set; }

        internal string Namespace { get; set; }

        internal ReloadSettings ReloadSettings { get; set; }

        internal ILogger Logger { get; set; }
    }
}
