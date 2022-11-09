// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Logging.DynamicLogger.Test;

public class TestDynamicMessageProcessor : IDynamicMessageProcessor
{
    public bool ProcessCalled { get; set; }

    public string Process(string inputLogMessage)
    {
        ProcessCalled = true;
        return string.Empty;
    }
}
