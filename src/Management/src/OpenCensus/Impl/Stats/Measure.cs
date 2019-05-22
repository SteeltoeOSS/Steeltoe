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

using Steeltoe.Management.Census.Stats.Measures;
using System;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class Measure : IMeasure
    {
        internal const int NAME_MAX_LENGTH = 255;

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract string Unit { get; }

        public abstract M Match<M>(Func<IMeasureDouble, M> p0, Func<IMeasureLong, M> p1, Func<IMeasure, M> defaultFunction);
    }
}
