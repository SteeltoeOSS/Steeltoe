// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding;

public abstract class AbstractBindable : IBindable
{
    private static readonly ICollection<IBinding> _bindings = new List<IBinding>();
    private static readonly ICollection<string> _empty = new List<string>();

    protected AbstractBindable()
    {
    }

    protected AbstractBindable(Type binding)
    {
        BindingType = binding;
    }

    public virtual Type BindingType { get; }

    public virtual ICollection<string> Inputs => _empty;

    public virtual ICollection<string> Outputs => _empty;

    public virtual ICollection<IBinding> CreateAndBindInputs(IBindingService bindingService)
    {
        return _bindings;
    }

    public virtual ICollection<IBinding> CreateAndBindOutputs(IBindingService bindingService)
    {
        return _bindings;
    }

    public virtual object GetBoundInputTarget(string name)
    {
        return null;
    }

    public virtual object GetBoundOutputTarget(string name)
    {
        return null;
    }

    public virtual object GetBoundTarget(string name)
    {
        return null;
    }

    public virtual void UnbindInputs(IBindingService bindingService)
    {
    }

    public virtual void UnbindOutputs(IBindingService bindingService)
    {
    }
}