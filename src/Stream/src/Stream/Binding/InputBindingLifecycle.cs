// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Binding;

public class InputBindingLifecycle : AbstractBindingLifecycle
{
    internal List<IBinding> InputBindings = new();

    public override int Phase { get; } = int.MaxValue - 1000;

    public InputBindingLifecycle(IBindingService bindingService, IEnumerable<IBindable> bindables)
        : base(bindingService, bindables)
    {
    }

    protected override void DoStartWithBindable(IBindable bindable)
    {
        ICollection<IBinding> bindableBindings = bindable.CreateAndBindInputs(BindingService);

        if (bindableBindings != null)
        {
            InputBindings.AddRange(bindableBindings);
        }
    }

    protected override void DoStopWithBindable(IBindable bindable)
    {
        bindable.UnbindInputs(BindingService);
    }
}
