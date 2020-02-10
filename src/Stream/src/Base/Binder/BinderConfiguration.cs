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

using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binder
{
    public class BinderConfiguration
    {
        private readonly IBinderOptions _options;

        public BinderConfiguration(string binderType, string binderAssemblyPath, IBinderOptions options)
        {
            ConfigureClass = binderType ?? throw new ArgumentNullException(nameof(binderType));
            ConfigureAssembly = binderAssemblyPath;
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string ConfigureClass { get; }

        public string ConfigureAssembly { get; }

        public string ResolvedAssembly { get; set; }

        public IDictionary<string, object> Properties
        {
            get
            {
                return _options.Environment;
            }
        }

        public bool IsInheritEnvironment
        {
            get
            {
                return _options.InheritEnvironment;
            }
        }

        public bool IsDefaultCandidate
        {
            get
            {
                return _options.DefaultCandidate;
            }
        }
    }
}
