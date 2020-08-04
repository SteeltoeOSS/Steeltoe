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
        {
            if (targetObject == null)
            {
                return default;
            }

            if (targetObject is T)
            {
                return (T)targetObject;
            }

            return default;
        }

        public static Exception WrapInHandlingExceptionIfNecessary(IMessage message, string text, Exception ex)
        {
            if (!(ex is MessagingException) || ((MessagingException)ex).FailedMessage == null)
            {
                return new MessageHandlingException(message, text, ex);
            }

            return ex;
        }

        public static IIntegrationServices GetIntegrationServices(IApplicationContext context)
        {
            var services = context?.GetService<IIntegrationServices>();
            if (services == null)
            {
                services = new IntegrationServices(context);
            }

            return services;
        }
    }
}
