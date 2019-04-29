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

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public static class B3Constants
    {
        public const string XB3TraceId = "X-B3-TraceId";
        public const string XB3SpanId = "X-B3-SpanId";
        public const string XB3ParentSpanId = "X-B3-ParentSpanId";
        public const string XB3Sampled = "X-B3-Sampled";
        public const string XB3Flags = "X-B3-Flags";
    }
}
