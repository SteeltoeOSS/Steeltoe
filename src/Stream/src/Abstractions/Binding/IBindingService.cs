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
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding
{
    /// <summary>
    /// Handles binding of input/output targets by delegating to an underlying Binder.
    /// TODO: Try to make this internal interface
    /// </summary>
    public interface IBindingService
    {
        ICollection<IBinding> BindConsumer<T>(T inputChannel, string name);

        IBinding BindProducer<T>(T outputChannel, string name);

        void UnbindProducers(string outputName);

        void UnbindConsumers(string inputName);
    }
}
