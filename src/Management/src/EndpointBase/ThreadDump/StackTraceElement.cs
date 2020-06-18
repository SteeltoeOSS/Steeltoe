// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.


using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class StackTraceElement
    {
        [JsonPropertyName("className")]
        public string ClassName { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyName("methodName")]
        public string MethodName { get; set; }

        [JsonPropertyName("nativeMethod")]
        public bool IsNativeMethod { get; set; }
    }
}
