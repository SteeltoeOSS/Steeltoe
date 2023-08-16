// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Test.Support.Converter;

public sealed class SimpleTrade
{
    public string Ticker { get; set; }

    public long Quantity { get; set; }

    public decimal Price { get; set; }

    public string OrderType { get; set; }

    public string AccountName { get; set; }

    public bool BuyRequest { get; set; }

    public string UserName { get; set; }

    public string RequestId { get; set; }

    public override int GetHashCode()
    {
        return HashCode.Combine(AccountName, BuyRequest, OrderType, Price, Quantity, RequestId, Ticker, UserName);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not SimpleTrade other || GetType() != obj.GetType())
        {
            return false;
        }

#pragma warning disable S1067 // Expressions should not be too complex
        return AccountName == other.AccountName && BuyRequest == other.BuyRequest && OrderType == other.OrderType && Price == other.Price &&
            Quantity == other.Quantity && RequestId == other.RequestId && Ticker == other.Ticker && UserName == other.UserName;
#pragma warning restore S1067 // Expressions should not be too complex
    }
}
