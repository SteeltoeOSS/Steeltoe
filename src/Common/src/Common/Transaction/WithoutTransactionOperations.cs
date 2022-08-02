// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

internal sealed class WithoutTransactionOperations : ITransactionOperations
{
    public static readonly WithoutTransactionOperations Instance = new();

    private WithoutTransactionOperations()
    {
    }

    public T Execute<T>(Func<ITransactionStatus, T> action)
    {
        return action(new SimpleTransactionStatus(false));
    }

    public void ExecuteWithoutResult(Action<ITransactionStatus> action)
    {
        action(new SimpleTransactionStatus(false));
    }
}
