// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Binding;
using Xunit;

namespace Steeltoe.Stream.Test.Binding;

public class BindingLifecycleTests
{
    [Fact]
    public async Task TestInputBindingLifecycle()
    {
        var bindables = new Dictionary<string, IBindable>();

        IBindable bindableWithTwo = new TestBindable(new Mock<IBinding>(), new Mock<IBinding>());
        IBindable bindableWithThree = new TestBindable(new Mock<IBinding>(), new Mock<IBinding>(), new Mock<IBinding>());
        IBindable bindableEmpty = new TestBindable();

        bindables.Add("two", bindableWithTwo);
        bindables.Add("empty", bindableEmpty);
        bindables.Add("three", bindableWithThree);

        var lifecycle = new InputBindingLifecycle(new Mock<IBindingService>().Object, bindables.Values);
        await lifecycle.StartAsync();

        Assert.Equal(5, lifecycle.InputBindings.Count);

        await lifecycle.StopAsync();
    }

    [Fact]
    public async Task TestOutputBindingLifecycle()
    {
        var bindables = new Dictionary<string, IBindable>();

        IBindable bindableWithTwo = new TestBindable(new Mock<IBinding>(), new Mock<IBinding>());
        IBindable bindableWithThree = new TestBindable(new Mock<IBinding>(), new Mock<IBinding>(), new Mock<IBinding>());
        IBindable bindableEmpty = new TestBindable();

        bindables.Add("two", bindableWithTwo);
        bindables.Add("empty", bindableEmpty);
        bindables.Add("three", bindableWithThree);

        var lifecycle = new OutputBindingLifecycle(new Mock<IBindingService>().Object, bindables.Values);
        await lifecycle.StartAsync();

        Assert.Equal(5, lifecycle.OutputBindings.Count);

        await lifecycle.StopAsync();
    }

    private sealed class TestBindable : IBindable
    {
        private readonly List<IBinding> _bindings = new();

        public Type BindingType => throw new NotImplementedException();

        public ICollection<string> Inputs => throw new NotImplementedException();

        public ICollection<string> Outputs => throw new NotImplementedException();

        public TestBindable(params Mock<IBinding>[] bindings)
        {
            foreach (Mock<IBinding> mocks in bindings)
            {
                _bindings.Add(mocks.Object);
            }
        }

        public ICollection<IBinding> CreateAndBindInputs(IBindingService bindingService)
        {
            return _bindings;
        }

        public ICollection<IBinding> CreateAndBindOutputs(IBindingService bindingService)
        {
            return _bindings;
        }

        public object GetBoundInputTarget(string name)
        {
            throw new NotImplementedException();
        }

        public object GetBoundOutputTarget(string name)
        {
            throw new NotImplementedException();
        }

        public object GetBoundTarget(string name)
        {
            throw new NotImplementedException();
        }

        public void UnbindInputs(IBindingService bindingService)
        {
        }

        public void UnbindOutputs(IBindingService bindingService)
        {
        }
    }
}
