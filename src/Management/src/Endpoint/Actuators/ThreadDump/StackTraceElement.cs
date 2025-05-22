// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

public sealed class StackTraceElement
{
    [JsonPropertyName("moduleName")]
    public string? ModuleName { get; set; }

    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    [JsonPropertyName("methodName")]
    public string? MethodName { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("lineNumber")]
    public int? LineNumber { get; set; }

    [JsonPropertyName("columnNumber")]
    public int? ColumnNumber { get; set; }

    [JsonPropertyName("nativeMethod")]
    public bool IsNativeMethod { get; set; }

    public override string ToString()
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(ClassName) && !string.IsNullOrEmpty(MethodName))
        {
            builder.Append(ClassName);
            builder.Append('.');
            builder.Append(MethodName);
        }

        if (!string.IsNullOrEmpty(ModuleName))
        {
            if (builder.Length > 0)
            {
                builder.Append(" in ");
            }

            builder.Append(ModuleName);
        }

        if (!string.IsNullOrEmpty(FileName))
        {
            if (builder.Length > 0)
            {
                builder.Append(" at ");
            }

            builder.Append(FileName);

            if (LineNumber != null)
            {
                builder.Append('(');
                builder.Append(LineNumber);

                if (ColumnNumber != null)
                {
                    builder.Append(',');
                    builder.Append(ColumnNumber);
                }

                builder.Append(')');
            }
        }

        return builder.ToString();
    }
}
