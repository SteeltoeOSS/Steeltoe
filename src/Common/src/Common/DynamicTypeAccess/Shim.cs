// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.DynamicTypeAccess;

/// <summary>
/// Building block to provide statically-typed access to a <see cref="Type" /> that is dynamically loaded at runtime.
/// </summary>
internal abstract class Shim
{
    protected InstanceAccessor InstanceAccessor { get; }

    public Type DeclaredType => InstanceAccessor.DeclaredTypeAccessor.Type;
    public virtual object Instance => InstanceAccessor.Instance;

    protected Shim(InstanceAccessor instanceAccessor)
    {
        ArgumentNullException.ThrowIfNull(instanceAccessor);

        InstanceAccessor = instanceAccessor;
    }

    public override string? ToString()
    {
        return InstanceAccessor.Instance.ToString();
    }
}
