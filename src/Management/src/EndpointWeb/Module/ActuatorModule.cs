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
