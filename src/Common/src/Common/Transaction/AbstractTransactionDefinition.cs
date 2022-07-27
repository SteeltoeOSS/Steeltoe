// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

public abstract class AbstractTransactionDefinition : ITransactionDefinition
{
    public const int PROPAGATION_REQUIRED = 0;
    public const int PROPAGATION_SUPPORTS = 1;
    public const int PROPAGATION_MANDATORY = 2;
    public const int PROPAGATION_REQUIRES_NEW = 3;
    public const int PROPAGATION_NOT_SUPPORTED = 4;
    public const int PROPAGATION_NEVER = 5;
    public const int PROPAGATION_NESTED = 6;
    public const int ISOLATION_DEFAULT = -1;
    public const int ISOLATION_READ_UNCOMMITTED = 1;
    public const int ISOLATION_READ_COMMITTED = 2;
    public const int ISOLATION_REPEATABLE_READ = 4;
    public const int ISOLATION_SERIALIZABLE = 8;
    public const int TIMEOUT_DEFAULT = -1;

    public static ITransactionDefinition WithDefaults => StaticTransactionDefinition.INSTANCE;

    public virtual int PropagationBehavior { get; set; } = PROPAGATION_REQUIRED;

    public virtual int IsolationLevel { get; set; } = ISOLATION_DEFAULT;

    public virtual int Timeout { get; set; } = TIMEOUT_DEFAULT;

    public virtual bool IsReadOnly { get; set; } = false;

    public virtual string Name { get; set; } = null;
}