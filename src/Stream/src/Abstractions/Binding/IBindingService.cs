// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding;

/// <summary>
/// Handles binding of input/output targets by delegating to an underlying Binder.
/// TODO: Try to make this internal interface
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IBindingService
{
    ICollection<IBinding> BindConsumer<T>(T inputChannel, string name);

    IBinding BindProducer<T>(T outputChannel, string name);

    void UnbindProducers(string outputName);

    void UnbindConsumers(string inputName);
}