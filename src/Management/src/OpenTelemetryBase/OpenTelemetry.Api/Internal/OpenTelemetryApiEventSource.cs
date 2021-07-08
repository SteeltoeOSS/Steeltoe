#pragma warning disable SA1636 // File header copyright text should match

// <copyright file="OpenTelemetryApiEventSource.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
#pragma warning restore SA1636 // File header copyright text should match

using System.Diagnostics.Tracing;

namespace OpenTelemetry.Internal
{
    /// <summary>
    /// EventSource implementation for OpenTelemetry API.
    /// This is used for internal logging of this library.
    /// </summary>
    [EventSource(Name = "OpenTelemetry-Api")]
    internal class OpenTelemetryApiEventSource : EventSource
    {
        public static readonly OpenTelemetryApiEventSource Log = new ();

        [Event(7, Message = "Calling method '{0}' with invalid argument '{1}', issue '{2}'.", Level = EventLevel.Warning)]
        public void InvalidArgument(string methodName, string argumentName, string issue) => WriteEvent(7, methodName, argumentName, issue);
    }
}
