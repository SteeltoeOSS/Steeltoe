// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Binding;

public class AbstractBindableProxyFactory : AbstractBindable
{
    protected IDictionary<string, Bindable> bindables;
    protected IList<IBindingTargetFactory> bindingTargetFactories;

    protected Dictionary<string, Lazy<object>> boundInputTargets = new();
    protected Dictionary<string, Lazy<object>> boundOutputTargets = new();

    public override ICollection<string> Inputs => boundInputTargets.Keys;

    public override ICollection<string> Outputs => boundOutputTargets.Keys;

    public AbstractBindableProxyFactory(Type bindingType, IEnumerable<IBindingTargetFactory> bindingTargetFactories)
        : base(bindingType)
    {
        this.bindingTargetFactories = bindingTargetFactories.ToList();
        Initialize();
    }

    public override ICollection<IBinding> CreateAndBindInputs(IBindingService bindingService)
    {
        var bindings = new List<IBinding>();

        foreach (KeyValuePair<string, Lazy<object>> boundTarget in boundInputTargets)
        {
            ICollection<IBinding> result = bindingService.BindConsumer(boundTarget.Value.Value, boundTarget.Key);
            bindings.AddRange(result);
        }

        return bindings;
    }

    public override ICollection<IBinding> CreateAndBindOutputs(IBindingService bindingService)
    {
        var bindings = new List<IBinding>();

        foreach (KeyValuePair<string, Lazy<object>> boundTarget in boundOutputTargets)
        {
            IBinding result = bindingService.BindProducer(boundTarget.Value.Value, boundTarget.Key);
            bindings.Add(result);
        }

        return bindings;
    }

    public override void UnbindInputs(IBindingService bindingService)
    {
        foreach (KeyValuePair<string, Lazy<object>> boundTarget in boundInputTargets)
        {
            bindingService.UnbindConsumers(boundTarget.Key);
        }
    }

    public override void UnbindOutputs(IBindingService bindingService)
    {
        foreach (KeyValuePair<string, Lazy<object>> boundTarget in boundOutputTargets)
        {
            bindingService.UnbindProducers(boundTarget.Key);
        }
    }

    public override object GetBoundTarget(string name)
    {
        object result = GetBoundInputTarget(name) ?? GetBoundOutputTarget(name);
        return result;
    }

    public override object GetBoundInputTarget(string name)
    {
        boundInputTargets.TryGetValue(name, out Lazy<object> result);
        return result.Value;
    }

    public override object GetBoundOutputTarget(string name)
    {
        boundOutputTargets.TryGetValue(name, out Lazy<object> result);
        return result.Value;
    }

    protected void Initialize()
    {
        bindables = BindingHelpers.CollectBindables(BindingType);

        foreach (Bindable bindable in bindables.Values)
        {
            IBindingTargetFactory factory = GetBindingTargetFactory(bindable.BindingTargetType);

            if (bindable.IsInput)
            {
                var creator = new Lazy<object>(() => factory.CreateInput(bindable.Name));
                boundInputTargets.Add(bindable.Name, creator);
            }
            else
            {
                var creator = new Lazy<object>(() => factory.CreateOutput(bindable.Name));
                boundOutputTargets.Add(bindable.Name, creator);
            }
        }
    }

    protected virtual IBindingTargetFactory GetBindingTargetFactory(Type bindingTargetType)
    {
        IBindingTargetFactory result = null;

        foreach (IBindingTargetFactory factory in bindingTargetFactories)
        {
            if (factory.CanCreate(bindingTargetType))
            {
                if (result == null)
                {
                    result = factory;
                }
                else
                {
                    throw new InvalidOperationException($"Multiple factories found for binding target type: {bindingTargetType}");
                }
            }
        }

        if (result == null)
        {
            throw new InvalidOperationException($"No factory found for binding target type: {bindingTargetType}");
        }

        return result;
    }
}
