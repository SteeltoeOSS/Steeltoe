// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

public class LogFileEndpointResponse(string? content, long? contentLength, Encoding? logFileEncoding, DateTime? lastModified)
{
    public string? Content { get; } = content;
    public Encoding? LogFileEncoding { get; } = logFileEncoding;
    public long? ContentLength { get; } = contentLength;
    public DateTime? LastModified { get; } = lastModified;
    public LogFileEndpointResponse()
        : this(null, null, null, null)
    {
    }
}
