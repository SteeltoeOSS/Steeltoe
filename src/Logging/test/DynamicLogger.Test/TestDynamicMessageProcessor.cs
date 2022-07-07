// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Logging.Test;

public class TestDynamicMessageProcessor : IDynamicMessageProcessor
{
    public bool ProcessCalled { get; set; }

    public string Process(string input)
    {
        ProcessCalled = true;
        return string.Empty;
    }
}
