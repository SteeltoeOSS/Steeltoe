// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Binding;

public interface IFooBinding
{
    [Input("input1")]
    ISubscribableChannel In1();

    [Input("input2")]
    ISubscribableChannel In2();

    [Output("output1")]
    IMessageChannel Out1();

    [Output("output2")]
    IMessageChannel Out2();

    [Input("inputXyz")]
    ISubscribableChannel InXyz();

    [Input("inputFooBar")]
    ISubscribableChannel InFooBar();

    [Input("inputFooBarBuzz")]
    ISubscribableChannel InFooBarBuzz();

    [Input("input_snake_case")]
    ISubscribableChannel InWithSnakeCase();
}
