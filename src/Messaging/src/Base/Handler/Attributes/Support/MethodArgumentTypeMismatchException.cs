// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Handler.Invocation;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    public class MethodArgumentTypeMismatchException : MethodArgumentResolutionException
    {
        public MethodArgumentTypeMismatchException(IMessage message, ParameterInfo parameter, string description)
        : base(message, parameter, description)
        {
        }
    }
}
