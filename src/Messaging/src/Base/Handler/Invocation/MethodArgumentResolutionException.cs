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

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public class MethodArgumentResolutionException : MessagingException
    {
        public ParameterInfo Parameter { get; private set; }

        public MethodArgumentResolutionException(IMessage message, ParameterInfo parameter)
            : base(message, GetMethodParameterMessage(parameter))
        {
            Parameter = parameter;
        }

        public MethodArgumentResolutionException(IMessage message, ParameterInfo parameter, string description)
            : base(message, GetMethodParameterMessage(parameter) + ": " + description)
        {
            Parameter = parameter;
        }

        private static string GetMethodParameterMessage(ParameterInfo parameter)
        {
            return "Could not resolve method parameter at index " + parameter.Position + " in " + parameter.Member.ToString();
        }
    }
}
