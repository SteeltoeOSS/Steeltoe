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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public abstract class HysrixCollapserBase
    {
        // this is a micro-optimization but saves about 1-2microseconds (on 2011 MacBook Pro)
        // on the repetitive string processing that will occur on the same classes over and over again
        protected static readonly ConcurrentDictionary<Type, string> _defaultNameCache = new ConcurrentDictionary<Type, string>();

        protected HysrixCollapserBase()
        {
        }
    }
}
