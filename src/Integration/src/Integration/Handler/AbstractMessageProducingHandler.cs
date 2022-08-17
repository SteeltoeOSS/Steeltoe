// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;

namespace Steeltoe.Integration.Handler;

public abstract class AbstractMessageProducingHandler : AbstractMessageHandler, IMessageProducer, IHeaderPropagation
{
    private readonly MessagingTemplate _messagingTemplate;

    private string _outputChannelName;

    private IMessageChannel _outputChannel;

    private List<string> _notPropagatedHeaders = new();

    private bool _noHeadersPropagation;

    private bool _selectiveHeaderPropagation;

    protected virtual bool ShouldCopyRequestHeaders => true;

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
        set => _outputChannel = value;
    }

    public virtual string OutputChannelName
    {
        get => _outputChannelName;
        set
        {
            ArgumentGuard.NotNullOrEmpty(value);

            _outputChannelName = value;
        }
    }

    public virtual IList<string> NotPropagatedHeaders
    {
        get => new List<string>(_notPropagatedHeaders);
        set => UpdateNotPropagatedHeaders(value, false);
    }

    public virtual int SendTimeout
    {
        get => _messagingTemplate.SendTimeout;
        set => _messagingTemplate.SendTimeout = value;
    }

    protected AbstractMessageProducingHandler(IApplicationContext context)
        : base(context)
    {
        _messagingTemplate = new MessagingTemplate(context);
    }

    public virtual void AddNotPropagatedHeaders(params string[] headers)
    {
        UpdateNotPropagatedHeaders(headers, true);
    }

    protected virtual void UpdateNotPropagatedHeaders(IList<string> headers, bool merge)
    {
        ArgumentGuard.NotNull(headers);
        ArgumentGuard.ElementsNotNullOrEmpty(headers);

        var headerPatterns = new HashSet<string>();

        if (merge && _notPropagatedHeaders.Count > 0)
        {
            foreach (string h in _notPropagatedHeaders)
            {
                headerPatterns.Add(h);
            }
        }

        if (headers.Count > 0)
        {
            foreach (string header in headers)
            {
                headerPatterns.Add(header);
            }

            _notPropagatedHeaders = headerPatterns.ToList();
        }

        bool hasAsterisk = headerPatterns.Contains("*");

        if (hasAsterisk)
        {
            _notPropagatedHeaders = new List<string>
            {
                "*"
            };

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
                foreach (object r in multiReply)
                {
                    ProduceOutput(r, requestMessage);
                }
            }
        }

        ProduceOutput(reply, requestMessage);
    }

    protected virtual void ProduceOutput(object reply, IMessage requestMessage)
    {
        IMessageHeaders requestHeaders = requestMessage.Headers;
        object replyChannel = null;

        if (OutputChannel == null)
        {
            replyChannel = ObtainReplyChannel(requestMessage.Headers, reply);
        }

        DoProduceOutput(requestHeaders, reply, replyChannel);
    }

    protected virtual void SendOutput(object output, object replyChannelArg, bool useArgChannel)
    {
        object replyChannel = replyChannelArg;
        IMessageChannel outChannel = OutputChannel;

        if (!useArgChannel && outChannel != null)
        {
            replyChannel = outChannel;
        }

        if (replyChannel == null)
        {
            throw new DestinationResolutionException("no output-channel or replyChannel header available");
        }

        var outputAsMessage = output as IMessage;

        switch (replyChannel)
        {
            case IMessageChannel channel:
                if (outputAsMessage != null)
                {
                    _messagingTemplate.Send(channel, outputAsMessage);
                }
                else
                {
                    _messagingTemplate.ConvertAndSend(channel, output);
                }

                break;
            case string strChannel:
                if (outputAsMessage != null)
                {
                    _messagingTemplate.Send(strChannel, outputAsMessage);
                }
                else
                {
                    _messagingTemplate.ConvertAndSend(strChannel, output);
                }

                break;
            default:
                throw new MessagingException("replyChannel must be a IMessageChannel or String");
        }
    }

    protected virtual bool ShouldSplitOutput(IEnumerable reply)
    {
        foreach (object next in reply)
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

            builder = IntegrationServices.MessageBuilderFactory.FromMessage(outputAsMessage);
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
        object replyChannel = requestHeaders.ReplyChannel;

        if (replyChannel == null)
        {
            if (reply is IMessage replyAsMessage)
            {
                replyChannel = replyAsMessage.Headers.ReplyChannel;
            }
            else if (reply is IMessageBuilder replyAsBuilder)
            {
                replyAsBuilder.Headers.TryGetValue(MessageHeaders.ReplyChannelName, out replyChannel);
            }
        }

        return replyChannel;
    }

    private void DoProduceOutput(IMessageHeaders requestHeaders, object reply, object replyChannel)
    {
        SendOutput(CreateOutputMessage(reply, requestHeaders), replyChannel, false);
    }
}
