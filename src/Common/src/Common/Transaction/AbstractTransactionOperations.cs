// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Transaction;

public abstract class AbstractTransactionOperations : ITransactionOperations
{
    public static ITransactionOperations WithoutTransaction()
    {
        return WithoutTransactionOperations.INSTANCE;
    }

    public abstract T Execute<T>(Func<ITransactionStatus, T> action);

    public virtual void ExecuteWithoutResult(Action<ITransactionStatus> action)
    {
        Execute<object>(status =>
        {
            action(status);
            return null;
        });
    }
}