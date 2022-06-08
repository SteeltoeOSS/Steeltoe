// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Retry;

public class TerminatedRetryException : RetryException
{
    public TerminatedRetryException(string msg, Exception cause)
        : base(msg, cause)
    {
    }

    public TerminatedRetryException(string msg)
        : base(msg)
    {
    }
}
