// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Handler;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace Steeltoe.Management.Endpoint.Module
{
    public class ActuatorModule : IHttpModule
    {
        protected ILogger<ActuatorModule> _logger;
        protected IList<IActuatorHandler> _handlers;

        public ActuatorModule()
            : base()
        {
        }

        public ActuatorModule(IList<IActuatorHandler> handlers, ILogger<ActuatorModule> logger)
        {
            _logger = logger;
            _handlers = handlers;
        }

        public virtual void Dispose()
        {
        }

        public virtual void Init(HttpApplication context)
        {
            if (_logger == null)
            {
                _logger = ActuatorConfigurator.LoggerFactory?.CreateLogger<ActuatorModule>();
            }

            if (_handlers == null)
            {
                _handlers = ActuatorConfigurator.ConfiguredHandlers;
            }

            EventHandlerTaskAsyncHelper asyncHelper = new EventHandlerTaskAsyncHelper(FilterAndPreProcessRequest);
            context.AddOnPostAuthorizeRequestAsync(asyncHelper.BeginEventHandler, asyncHelper.EndEventHandler);
        }

        public virtual async Task FilterAndPreProcessRequest(HttpContextBase context, Action completeRequest)
        {
            if (_handlers == null)
            {
                return;
            }

            foreach (var handler in _handlers)
            {
                if (handler.RequestVerbAndPathMatch(context.Request.HttpMethod, context.Request.Path))
                {
                    if (await handler.IsAccessAllowed(context).ConfigureAwait(false))
                    {
                        handler.HandleRequest(context);
                    }

                    completeRequest();
                    break;
                }
            }
        }

        protected virtual async Task FilterAndPreProcessRequest(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            var contextWrapper = new HttpContextWrapper(application.Context);
            await FilterAndPreProcessRequest(contextWrapper, HttpContext.Current.ApplicationInstance.CompleteRequest).ConfigureAwait(false);
         }
    }
}
