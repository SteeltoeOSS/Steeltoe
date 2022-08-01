// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Binding;

/// <summary>
/// Marker interface for instances that can bind/unbind groups of inputs and outputs.
/// TODO: Try to make this internal.
/// </summary>
public interface IBindable
{
    Type BindingType { get; }

    ICollection<string> Inputs { get; }

    ICollection<string> Outputs { get; }

    ICollection<IBinding> CreateAndBindInputs(IBindingService bindingService);

    ICollection<IBinding> CreateAndBindOutputs(IBindingService bindingService);

    void UnbindInputs(IBindingService bindingService);

    void UnbindOutputs(IBindingService bindingService);

    object GetBoundTarget(string name);

    object GetBoundInputTarget(string name);

    object GetBoundOutputTarget(string name);
}
