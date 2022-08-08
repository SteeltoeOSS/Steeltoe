// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

public class TransactionSystemException : TransactionException
{
    public Exception ApplicationException { get; private set; }

    public Exception OriginalException => ApplicationException ?? InnerException;

    public TransactionSystemException(string msg)
        : base(msg)
    {
    }

    public TransactionSystemException(string msg, Exception cause)
        : base(msg, cause)
    {
    }

    public void InitApplicationException(Exception exception)
    {
        ArgumentGuard.NotNull(exception);

        if (ApplicationException != null)
        {
            throw new InvalidOperationException($"Already holding an application exception: {ApplicationException}");
        }

        ApplicationException = exception;
    }

    public bool Contains(Type exceptionType)
    {
        return exceptionType != null && exceptionType.IsInstanceOfType(ApplicationException);
    }
}
