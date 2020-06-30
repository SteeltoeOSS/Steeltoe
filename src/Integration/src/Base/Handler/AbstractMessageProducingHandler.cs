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

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Integration.Handler
{
    public abstract class AbstractMessageProducingHandler : AbstractMessageHandler, IMessageProducer, IHeaderPropagation
    {
        private readonly MessagingTemplate _messagingTemplate;

        private string _outputChannelName;

        private IMessageChannel _outputChannel;

        private List<string> _notPropagatedHeaders = new List<string>();

        private bool _noHeadersPropagation = false;

        private bool _selectiveHeaderPropagation = false;

        protected AbstractMessageProducingHandler(IApplicationContext context)
            : base(context)
        {
            _messagingTemplate = new MessagingTemplate(context);
        }

        public virtual IMessageChannel OutputChannel
        {
            get
            {
                if (_outputChannelName != null)
                {
                    _outputChannel = IntegrationServices.ChannelResolver.ResolveDestination(_outputChannelName);
                    _outputChannelName = null;
                }

                return _outputChannel;
            }

            set
            {
                _outputChannel = value;
            }
        }

        public virtual string OutputChannelName
        {
            get
            {
                return _outputChannelName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("outputChannelName must not be empty");
                }

                _outputChannelName = value;
            }
        }

        public virtual IList<string> NotPropagatedHeaders
        {
            get
            {
                return new List<string>(_notPropagatedHeaders);
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                UpdateNotPropagatedHeaders(value, false);
            }
        }

        public virtual void AddNotPropagatedHeaders(params string[] headers)
        {
            UpdateNotPropagatedHeaders(headers, true);
        }

        protected virtual bool ShouldCopyRequestHeaders
        {
            get
            {
                return true;
            }
        }

        protected virtual int SendTimeout
        {
            get { return _messagingTemplate.SendTimeout; }
            set { _messagingTemplate.SendTimeout = value; }
        }

        protected virtual void UpdateNotPropagatedHeaders(IList<string> headers, bool merge)
        {
            var headerPatterns = new HashSet<string>();

            if (merge && _notPropagatedHeaders.Count > 0)
            {
                foreach (var h in _notPropagatedHeaders)
                {
                    headerPatterns.Add(h);
                }
            }

            if (headers.Count > 0)
            {
                foreach (var h in headers)
                {
                    if (string.IsNullOrEmpty(h))
                    {
                        throw new ArgumentException("null or empty elements are not allowed in 'headers'");
                    }

                    headerPatterns.Add(h);
                }

                _notPropagatedHeaders = headerPatterns.ToList();
            }

            var hasAsterisk = headerPatterns.Contains("*");

            if (hasAsterisk)
            {
                _notPropagatedHeaders = new List<string>() { "*" };
                _noHeadersPropagation = true;
            }

            if (_notPropagatedHeaders.Count > 0)
            {
                _selectiveHeaderPropagation = true;
            }
        }

        protected void SendOutputs(object reply, IMessage requestMessage)
        {
            if (reply == null)
            {
                return;
            }

            if (IsEnumerable(reply))
            {
                var multiReply = (IEnumerable)reply;
                if (ShouldSplitOutput(multiReply))
                {
                    foreach (var r in multiReply)
                    {
                        ProduceOutput(r, requestMessage);
                    }
                }
            }

            ProduceOutput(reply, requestMessage);
        }

        protected virtual void ProduceOutput(object reply, IMessage requestMessage)
        {
            var requestHeaders = requestMessage.Headers;
            object replyChannel = null;
            if (OutputChannel == null)
            {
                replyChannel = ObtainReplyChannel(requestMessage.Headers, reply);
            }

            DoProduceOutput(requestHeaders, reply, replyChannel);
        }

        protected virtual void SendOutput(object output, object replyChannelArg, bool useArgChannel)
        {
            var replyChannel = replyChannelArg;
            var outChannel = OutputChannel;
            if (!useArgChannel && outChannel != null)
            {
                replyChannel = outChannel;
            }

            if (replyChannel == null)
            {
                throw new DestinationResolutionException("no output-channel or replyChannel header available");
            }

            var outputAsMessage = output as IMessage;

            if (replyChannel is IMessageChannel)
            {
                if (outputAsMessage != null)
                {
                    _messagingTemplate.Send((IMessageChannel)replyChannel, outputAsMessage);
                }
                else
                {
                    _messagingTemplate.ConvertAndSend((IMessageChannel)replyChannel, output);
                }
            }
            else if (replyChannel is string)
            {
                if (outputAsMessage != null)
                {
                    _messagingTemplate.Send((string)replyChannel, outputAsMessage);
                }
                else
                {
                    _messagingTemplate.ConvertAndSend((string)replyChannel, output);
                }
            }
            else
            {
                throw new MessagingException("replyChannel must be a IMessageChannel or String");
            }
        }

        protected virtual bool ShouldSplitOutput(IEnumerable reply)
        {
            foreach (var next in reply)
            {
                if (next is IMessage)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual IMessage CreateOutputMessage(object output, IMessageHeaders requestHeaders)
        {
            IMessageBuilder builder;
            if (output is IMessage outputAsMessage)
            {
                if (_noHeadersPropagation || !ShouldCopyRequestHeaders)
                {
                    return outputAsMessage;
                }

                builder = IntegrationServices.MessageBuilderFactory.FromMessage((IMessage)output);
            }
            else
            {
                builder = IntegrationServices.MessageBuilderFactory.WithPayload(output);
            }

            if (!_noHeadersPropagation && ShouldCopyRequestHeaders)
            {
                builder.FilterAndCopyHeadersIfAbsent(requestHeaders, _selectiveHeaderPropagation ? _notPropagatedHeaders.ToArray() : null);
            }

            return builder.Build();
        }

        private bool IsEnumerable(object reply)
        {
            if (reply is string || reply is Array)
            {
                return false;
            }

            if (reply is IEnumerable)
            {
                return true;
            }

            return false;
        }

        private object ObtainReplyChannel(IMessageHeaders requestHeaders, object reply)
        {
            var replyChannel = requestHeaders.ReplyChannel;

            if (replyChannel == null)
            {
                if (reply is IMessage replyAsMessage)
                {
                    replyChannel = replyAsMessage.Headers.ReplyChannel;
                }
                else if (reply is IMessageBuilder replyAsBuilder)
                {
                    replyAsBuilder.Headers.TryGetValue(MessageHeaders.REPLY_CHANNEL, out replyChannel);
                }
            }

            return replyChannel;
        }

        private void DoProduceOutput(IMessageHeaders requestHeaders, object reply, object replyChannel)
        {
            SendOutput(CreateOutputMessage(reply, requestHeaders), replyChannel, false);
        }
    }
}
