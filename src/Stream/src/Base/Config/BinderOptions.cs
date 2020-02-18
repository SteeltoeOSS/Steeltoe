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

using System.Collections.Generic;

namespace Steeltoe.Stream.Config
{
    public class BinderOptions : IBinderOptions
    {
        private const bool InheritEnvironment_Default = true;
        private const bool DefaultCandidate_Default = true;

        public BinderOptions()
        {
        }

        public string ConfigureClass { get; set; }

        public string ConfigureAssembly { get; set; }

        public Dictionary<string, object> Environment { get; set; }

        public bool? InheritEnvironment { get; set; }

        public bool? DefaultCandidate { get; set; }

        bool IBinderOptions.InheritEnvironment => InheritEnvironment.Value;

        bool IBinderOptions.DefaultCandidate => DefaultCandidate.Value;

        internal void PostProcess()
        {
            if (Environment == null)
            {
                Environment = new Dictionary<string, object>();
            }

            if (!InheritEnvironment.HasValue)
            {
                InheritEnvironment = InheritEnvironment_Default;
            }

            if (!DefaultCandidate.HasValue)
            {
                DefaultCandidate = DefaultCandidate_Default;
            }
        }
    }
}
