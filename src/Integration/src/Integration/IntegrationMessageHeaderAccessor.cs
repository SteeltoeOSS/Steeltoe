// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Acks;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration;

public class IntegrationMessageHeaderAccessor : MessageHeaderAccessor
{
    public const string CorrelationId = "correlationId";

    public const string ExpirationDate = "expirationDate";

    public const string Priority = "priority";

    public const string SequenceNumber = "sequenceNumber";

    public const string SequenceSize = "sequenceSize";

    public const string SequenceDetails = "sequenceDetails";

    public const string RoutingSlip = "routingSlip";

    public const string DuplicateMessage = "duplicateMessage";

    public const string CloseableResource = "closeableResource";

    public const string DeliveryAttempt = "deliveryAttempt";

    public const string AcknowledgmentCallback = "acknowledgmentCallback";

    public const string SourceData = "sourceData";

    private ISet<string> _readOnlyHeaders = new HashSet<string>();

    public IntegrationMessageHeaderAccessor(IMessage message)
        : base(message)
    {
    }

    public void SetReadOnlyHeaders(IList<string> readOnlyHeaders)
    {
        foreach (string h in readOnlyHeaders)
        {
            if (h == null)
            {
                throw new ArgumentException("'readOnlyHeaders' must not be contain null items.");
            }
        }

        if (readOnlyHeaders.Count > 0)
        {
            _readOnlyHeaders = new HashSet<string>(readOnlyHeaders);
        }
    }

    public long? GetExpirationDate()
    {
        return (long?)GetHeader(ExpirationDate);
    }

    public object GetCorrelationId()
    {
        return GetHeader(CorrelationId);
    }

    public int GetSequenceNumber()
    {
        int? sequenceNumber = (int?)GetHeader(SequenceNumber);
        return sequenceNumber ?? 0;
    }

    public int GetSequenceSize()
    {
        int? sequenceSize = (int?)GetHeader(SequenceSize);
        return sequenceSize ?? 0;
    }

    public int? GetPriority()
    {
        return (int?)GetHeader(Priority);
    }

    public IAcknowledgmentCallback GetAcknowledgmentCallback()
    {
        return (IAcknowledgmentCallback)GetHeader(AcknowledgmentCallback);
    }

    public int? GetDeliveryAttempt()
    {
        return (int?)GetHeader(DeliveryAttempt);
    }

    public T GetHeader<T>(string key)
    {
        object value = GetHeader(key);

        if (value == null)
        {
            return default;
        }

        if (value is not T typedValue)
        {
            throw new ArgumentException($"Incorrect type specified for header '{key}'. Expected [{typeof(T)}] but actual type is [{value.GetType()}]");
        }

        return typedValue;
    }

    public override IDictionary<string, object> ToDictionary()
    {
        if (_readOnlyHeaders.Count == 0)
        {
            return base.ToDictionary();
        }

        IDictionary<string, object> headers = base.ToDictionary();

        foreach (string header in _readOnlyHeaders)
        {
            headers.Remove(header);
        }

        return headers;
    }

    public new bool IsReadOnly(string headerName)
    {
        return base.IsReadOnly(headerName) || _readOnlyHeaders.Contains(headerName);
    }

    protected override void VerifyType(string headerName, object headerValue)
    {
        if (headerName != null && headerValue != null)
        {
            base.VerifyType(headerName, headerValue);

            if (ExpirationDate.Equals(headerName))
            {
                if (!(headerValue is DateTime || headerValue is long))
                {
                    throw new ArgumentException($"The '{headerName}' header value must be a Date or Long.");
                }
            }
            else if (SequenceNumber.Equals(headerName) || SequenceSize.Equals(headerName) || Priority.Equals(headerName))
            {
                if (headerValue is not int)
                {
                    throw new ArgumentException($"The '{headerName}' header value must be a integer.");
                }
            }
            else if (RoutingSlip.Equals(headerName))
            {
                if (headerValue is not IDictionary<string, object>)
                {
                    throw new ArgumentException($"The '{headerName}' header value must be a IDictionary<string,object>.");
                }
            }
            else if (DuplicateMessage.Equals(headerName) && headerValue is not bool)
            {
                throw new ArgumentException($"The '{headerName}' header value must be a boolean.");
            }
        }
    }
}
