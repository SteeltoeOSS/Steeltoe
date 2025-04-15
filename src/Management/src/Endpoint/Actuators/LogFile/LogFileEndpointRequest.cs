// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

public class LogFileEndpointRequest(long? rangeStartByte, long? rangeLastByte, bool readContent)
{
    public long? RangeStartByte { get; } = rangeStartByte;
    public long? RangeLastByte { get; } = rangeLastByte;
    public bool ReadContent { get; } = readContent;

    public LogFileEndpointRequest()
        : this(null, null, true)
    {
    }

    public LogFileEndpointRequest(long? rangeStartByte, long? rangeLastByte)
        : this(rangeStartByte, rangeLastByte, true)
    {
    }
}
