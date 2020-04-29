﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration.Json;
using System;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    internal class JsonStreamConfigurationProvider : JsonConfigurationProvider
    {
        private readonly JsonStreamConfigurationSource _source;

        internal JsonStreamConfigurationProvider(JsonStreamConfigurationSource source)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
        }

        public override void Load()
        {
            Load(_source.Stream);
        }
    }
}
