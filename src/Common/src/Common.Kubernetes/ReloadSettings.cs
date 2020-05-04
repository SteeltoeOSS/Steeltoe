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

namespace Steeltoe.Common.Kubernetes
{
    public class ReloadSettings
    {
        /// <summary>
        /// Gets or sets the reload method (polling or event)
        /// </summary>
        public ReloadMethods Mode { get; set; } = ReloadMethods.Polling;

        /// <summary>
        /// Gets or sets the number of seconds before reloading config data
        /// </summary>
        public int Period { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating whether config maps should be reloaded if changed
        /// </summary>
        public bool ConfigMaps { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether secrets should be reloaded if changed
        /// </summary>
        public bool Secrets { get; set; } = false;
    }

    public enum ReloadMethods
    {
        Event,
        Polling
    }
}
