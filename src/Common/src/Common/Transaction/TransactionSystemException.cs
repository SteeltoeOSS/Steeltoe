// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Transaction;

public class TransactionSystemException : TransactionException
{
    public TransactionSystemException(string msg)
        : base(msg)
    {
    }

    public TransactionSystemException(string msg, Exception cause)
        : base(msg, cause)
    {
    }

    public Exception ApplicationException { get; private set; }

    public Exception OriginalException => ApplicationException ?? InnerException;

    public void InitApplicationException(Exception exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (ApplicationException != null)
        {
            throw new InvalidOperationException("Already holding an application exception: " + ApplicationException);
        }

        ApplicationException = exception;
    }

    public bool Contains(Type exceptionType)
    {
        return exceptionType != null && exceptionType.IsInstanceOfType(ApplicationException);
    }
}