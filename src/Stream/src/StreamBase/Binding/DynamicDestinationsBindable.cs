// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;
using System.Collections.Concurrent;

namespace Steeltoe.Stream.Binding;

public class DynamicDestinationsBindable : AbstractBindable
{
    private readonly ConcurrentDictionary<string, IBinding> _outputBindings = new ();

    public override Type BindingType => typeof(DynamicDestinationsBindable);

    public override ICollection<string> Outputs
    {
        get
        {
            return _outputBindings.Keys;
        }
    }

    public void AddOutputBinding(string name, IBinding binding)
    {
        _outputBindings.TryAdd(name, binding);
    }

    public override void UnbindOutputs(IBindingService bindingService)
    {
        foreach (var entry in _outputBindings)
        {
            _outputBindings.TryRemove(entry.Key, out var binding);
            if (binding != null)
            {
                binding.Unbind();
            }
        }
    }
}
