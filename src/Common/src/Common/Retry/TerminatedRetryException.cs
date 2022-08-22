// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Retry;

public class TerminatedRetryException : RetryException
{
    public TerminatedRetryException(string message)
        : base(message)
    {
    }

    public TerminatedRetryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
