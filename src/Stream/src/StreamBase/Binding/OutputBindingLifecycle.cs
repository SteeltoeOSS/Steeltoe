// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding
{
    public class OutputBindingLifecycle : AbstractBindingLifecycle
    {
        internal List<IBinding> _outputBindings = new List<IBinding>();

        public OutputBindingLifecycle(IBindingService bindingService, IEnumerable<IBindable> bindables)
            : base(bindingService, bindables)
        {
        }

        public override int Phase { get; } = int.MinValue + 1000;

        protected override void DoStartWithBindable(IBindable bindable)
        {
            var bindableBindings = bindable.CreateAndBindOutputs(_bindingService);
            _outputBindings.AddRange(bindableBindings);
        }

        protected override void DoStopWithBindable(IBindable bindable)
        {
            bindable.UnbindOutputs(_bindingService);
        }
    }
}
