// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

public interface ITransactionDefinition
{
    int PropagationBehavior { get; }

    int IsolationLevel { get; }

    int Timeout { get; }

    bool IsReadOnly { get; }

    string Name { get; }
}