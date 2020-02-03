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

namespace Steeltoe.Integration.Handler
{
    /// <summary>
    /// MessageHandlers implementing this interface can propagate headers from
    /// an input message to an output message.
    /// </summary>
    public interface IHeaderPropagation
    {
        /// <summary>
        /// Gets or sets the headers that should not be copied from inbound message if
        /// handler is configured to copy headers
        /// </summary>
        IList<string> NotPropagatedHeaders { get; set; }

        /// <summary>
        /// Add headers that will not be copied from the inbound message if
        /// handler is configured to copy headers
        /// </summary>
        /// <param name="headers">the headers to not copy</param>
        void AddNotPropagatedHeaders(params string[] headers);
    }
}
