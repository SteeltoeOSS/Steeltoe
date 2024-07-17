// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.DynamicTypeAccess;

/// <summary>
/// Provides a shim for a compiler-generated type that behaves like a <see cref="Task{TResult}" />, but is not assignable to it. For example:
/// <code><![CDATA[AsyncTaskMethodBuilder<T>.AsyncStateMachineBox<T>]]></code>.
/// </summary>
/// <typeparam name="TResult">
/// The type of the result produced by the underlying task.
/// </typeparam>
internal sealed class TaskShim<TResult> : Shim, IDisposable
{
    public override Task Instance => (Task)base.Instance;

    public TResult Result => InstanceAccessor.GetPropertyValue<TResult>("Result");

    public TaskShim(object instance)
        : base(Wrap(instance))
    {
    }

    private static InstanceAccessor Wrap(object instance)
    {
        Type taskLikeType = instance.GetType();
        var typeAccessor = new TypeAccessor(taskLikeType);
        return new InstanceAccessor(typeAccessor, instance);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
