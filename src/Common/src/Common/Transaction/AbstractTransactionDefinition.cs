// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

public abstract class AbstractTransactionDefinition : ITransactionDefinition
{
    public const int PropagationRequired = 0;
    public const int PropagationSupports = 1;
    public const int PropagationMandatory = 2;
    public const int PropagationRequiresNew = 3;
    public const int PropagationNotSupported = 4;
    public const int PropagationNever = 5;
    public const int PropagationNested = 6;
    public const int IsolationDefault = -1;
    public const int IsolationReadUncommitted = 1;
    public const int IsolationReadCommitted = 2;
    public const int IsolationRepeatableRead = 4;
    public const int IsolationSerializable = 8;
    public const int TimeoutDefault = -1;

    public static ITransactionDefinition WithDefaults => StaticTransactionDefinition.Instance;

    public virtual int PropagationBehavior { get; set; } = PropagationRequired;

    public virtual int IsolationLevel { get; set; } = IsolationDefault;

    public virtual int Timeout { get; set; } = TimeoutDefault;

    public virtual bool IsReadOnly { get; set; }

    public virtual string Name { get; set; }
}
