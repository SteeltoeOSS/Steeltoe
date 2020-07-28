using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Retry
{
    public class RepublishMessageRecoverer : IMessageRecoverer
    {
        public const string X_EXCEPTION_STACKTRACE = "x-exception-stacktrace";
        public const string X_EXCEPTION_MESSAGE = "x-exception-message";
        public const string X_ORIGINAL_EXCHANGE = "x-original-exchange";
        public const string X_ORIGINAL_ROUTING_KEY = "x-original-routingKey";
        public const int DEFAULT_FRAME_MAX_HEADROOM = 20_000;

        private const int ELLIPSIS_LENGTH = 3;
        private const int MAX_EXCEPTION_MESSAGE_SIZE_IN_TRACE = 100 - ELLIPSIS_LENGTH;

        public RepublishMessageRecoverer(RabbitTemplate errorTemplate)
            : this(errorTemplate, null, null)
        {
        }

        public RepublishMessageRecoverer(RabbitTemplate errorTemplate, string errorExchange)
        : this(errorTemplate, errorExchange, null)
        {
        }

        public RepublishMessageRecoverer(RabbitTemplate errorTemplate, string errorExchange, string errorRoutingKey)
        {
            if (errorTemplate == null)
            {
                throw new ArgumentNullException(nameof(errorTemplate));
            }

            ErrorTemplate = errorTemplate;
            ErrorExchangeName = errorExchange;
            ErrorRoutingKey = errorRoutingKey;
            MaxStackTraceLength = int.MaxValue;
        }

        public RabbitTemplate ErrorTemplate { get; }

        public string ErrorExchangeName { get; }

        public string ErrorRoutingKey { get; }

        public int MaxStackTraceLength { get; set; } = -1;

        public string ErrorRoutingKeyPrefix { get; set; }

        public MessageDeliveryMode DeliveryMode { get; set; }

        public int FrameMaxHeadroom { get; set; } = DEFAULT_FRAME_MAX_HEADROOM;

        public void Recover(IMessage message, Exception exception)
        {
            var headers = RabbitHeaderAccessor.GetMutableAccessor(message);

            var exceptionMessage = exception.InnerException != null ? exception.InnerException.Message : exception.Message;
            List<string> processed = ProcessStackTrace(exception, exceptionMessage);
            var stackTraceAsString = processed[0];
            var truncatedExceptionMessage = processed[1];
            if (truncatedExceptionMessage != null)
            {
                exceptionMessage = truncatedExceptionMessage;
            }

            headers.SetHeader(X_EXCEPTION_STACKTRACE, stackTraceAsString);
            headers.SetHeader(X_EXCEPTION_MESSAGE, exceptionMessage);
            headers.SetHeader(X_ORIGINAL_EXCHANGE, headers.ReceivedExchange);
            headers.SetHeader(X_ORIGINAL_ROUTING_KEY, headers.ReceivedRoutingKey);
            var additionalHeaders = AddAdditionalHeaders(message, exception);

            if (additionalHeaders != null)
            {
                foreach (var h in additionalHeaders)
                {
                    headers.SetHeader(h.Key, h.Value);
                }
            }

            if (headers.DeliveryMode == null)
            {
                headers.DeliveryMode = DeliveryMode;
            }

            if (ErrorExchangeName != null)
            {
                var routingKey = ErrorRoutingKey != null ? ErrorRoutingKey : PrefixedOriginalRoutingKey(message);
                ErrorTemplate.Send(ErrorExchangeName, routingKey, message);

                // this.logger.warn("Republishing failed message to exchange '" + this.errorExchangeName  + "' with routing key " + routingKey);
            }
            else
            {
                var routingKey = PrefixedOriginalRoutingKey(message);
                ErrorTemplate.Send(routingKey, message);

                // logger.warn("Republishing failed message to the template's default exchange with routing key " + routingKey);
            }
        }

        protected virtual Dictionary<string, object> AddAdditionalHeaders(IMessage message, Exception cause)
        {
            return null;
        }

        private string PrefixedOriginalRoutingKey(IMessage message)
        {
            return ErrorRoutingKeyPrefix + message.Headers.ReceivedRoutingKey();
        }

        private List<string> ProcessStackTrace(Exception cause, string exceptionMessage)
        {
            var stackTraceAsString = cause.StackTrace;
            if (MaxStackTraceLength < 0)
            {
                int maxStackTraceLen = RabbitUtils.GetMaxFrame(ErrorTemplate.ConnectionFactory);
                if (maxStackTraceLen > 0)
                {
                    maxStackTraceLen -= FrameMaxHeadroom;
                    MaxStackTraceLength = maxStackTraceLen;
                }
            }

            return TruncateIfNecessary(cause, exceptionMessage, stackTraceAsString);
        }

        private List<string> TruncateIfNecessary(Exception cause, string exception, string stackTrace)
        {
            var truncated = false;
            var stackTraceAsString = stackTrace;
            var exceptionMessage = exception;
            var truncatedExceptionMessage = exceptionMessage.Length <= MAX_EXCEPTION_MESSAGE_SIZE_IN_TRACE ? exceptionMessage : (exceptionMessage.Substring(0, MAX_EXCEPTION_MESSAGE_SIZE_IN_TRACE) + "...");
            if (MaxStackTraceLength > 0 && stackTraceAsString.Length + exceptionMessage.Length > MaxStackTraceLength)
            {

                if (!exceptionMessage.Equals(truncatedExceptionMessage))
                {
                    int start = stackTraceAsString.IndexOf(exceptionMessage);
                    stackTraceAsString = stackTraceAsString.Substring(0, start)
                            + truncatedExceptionMessage
                            + stackTraceAsString.Substring(start + exceptionMessage.Length);
                }

                int adjustedStackTraceLen = MaxStackTraceLength - truncatedExceptionMessage.Length;
                if (adjustedStackTraceLen > 0)
                {
                    if (stackTraceAsString.Length > adjustedStackTraceLen)
                    {
                        stackTraceAsString = stackTraceAsString.Substring(0, adjustedStackTraceLen);
                        //logger.warn("Stack trace in republished message header truncated due to frame_max "
                        //        + "limitations; "
                        //        + "consider increasing frame_max on the broker or reduce the stack trace depth", cause);
                        truncated = true;
                    }
                    else if (stackTraceAsString.Length + exceptionMessage.Length > MaxStackTraceLength)
                    {
                        //this.logger.warn("Exception message in republished message header truncated due to frame_max "
                        //        + "limitations; consider increasing frame_max on the broker or reduce the exception "
                        //        + "message size", cause);
                        truncatedExceptionMessage = exceptionMessage.Substring(0, MaxStackTraceLength - stackTraceAsString.Length - ELLIPSIS_LENGTH) + "...";
                        truncated = true;
                    }
                }
            }

            return new List<string>() { stackTraceAsString, truncated ? truncatedExceptionMessage : null };
        }
    }
}
