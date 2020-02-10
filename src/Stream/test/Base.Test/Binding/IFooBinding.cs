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

using Steeltoe.Messaging;
using Steeltoe.Stream.Attributes;

namespace Steeltoe.Stream.Binding
{
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
}
