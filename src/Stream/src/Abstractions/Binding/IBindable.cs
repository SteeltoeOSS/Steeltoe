// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Stream.Binder;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding
{
    /// <summary>
    /// Marker interface for instances that can bind/unbind groups of inputs and outputs.
    /// TODO: Try to make this internal
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
}
