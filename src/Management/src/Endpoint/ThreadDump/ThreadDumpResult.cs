// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpResult
{
    [JsonPropertyName("threads")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4004:Collection properties should be readonly", Justification = "Allow in Options")]
    public IList<ThreadInfo> Threads { get; set; }
}
