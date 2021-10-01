// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Util
{
    public static class IntegrationServicesUtils
    {
        public static T ExtractTypeIfPossible<T>(object targetObject)
            => targetObject switch
                {
                    T t => t,
                    _ => default
                };

        public static Exception WrapInHandlingExceptionIfNecessary(IMessage message, string text, Exception ex)
            => ex is not MessagingException exception || exception.FailedMessage == null
                ? new MessageHandlingException(message, text, ex)
                : ex;

        public static IIntegrationServices GetIntegrationServices(IApplicationContext context)
            => context?.GetService<IIntegrationServices>() ?? new IntegrationServices(context);
    }
}
