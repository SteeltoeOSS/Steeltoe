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
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ISpan
    {
        ISpanContext Context { get; }

        SpanOptions Options { get; }

        Status Status { get; set; }

        void PutAttribute(string key, IAttributeValue value);

        void PutAttributes(IDictionary<string, IAttributeValue> attributes);

        void AddAnnotation(string description);

        void AddAnnotation(string description, IDictionary<string, IAttributeValue> attributes);

        void AddAnnotation(IAnnotation annotation);

        void AddMessageEvent(IMessageEvent messageEvent);

        void AddLink(ILink link);

        void End(EndSpanOptions options);

        void End();
    }
}
