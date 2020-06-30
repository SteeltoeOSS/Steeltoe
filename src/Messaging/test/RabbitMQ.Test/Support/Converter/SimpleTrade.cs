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

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class SimpleTrade
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
            var prime = 31;
            var result = 1;
            result = (prime * result) + ((AccountName == null) ? 0 : AccountName.GetHashCode());
            result = (prime * result) + (BuyRequest ? 1231 : 1237);
            result = (prime * result) + ((OrderType == null) ? 0 : OrderType.GetHashCode());
            result = (prime * result) + Price.GetHashCode();
            result = (prime * result) + (int)(Quantity ^ (Quantity >> 32));
            result = (prime * result) + ((RequestId == null) ? 0 : RequestId.GetHashCode());
            result = (prime * result) + ((Ticker == null) ? 0 : Ticker.GetHashCode());
            result = (prime * result) + ((UserName == null) ? 0 : UserName.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            var other = (SimpleTrade)obj;
            if (AccountName == null)
            {
                if (other.AccountName != null)
                {
                    return false;
                }
            }
            else if (!AccountName.Equals(other.AccountName))
            {
                return false;
            }

            if (BuyRequest != other.BuyRequest)
            {
                return false;
            }

            if (OrderType == null)
            {
                if (other.OrderType != null)
                {
                    return false;
                }
            }
            else if (!OrderType.Equals(other.OrderType))
            {
                return false;
            }

            if (Price != other.Price)
            {
                return false;
            }

            if (Quantity != other.Quantity)
            {
                return false;
            }

            if (RequestId == null)
            {
                if (other.RequestId != null)
                {
                    return false;
                }
            }
            else if (!RequestId.Equals(other.RequestId))
            {
                return false;
            }

            if (Ticker == null)
            {
                if (other.Ticker != null)
                {
                    return false;
                }
            }
            else if (!Ticker.Equals(other.Ticker))
            {
                return false;
            }

            if (UserName == null)
            {
                if (other.UserName != null)
                {
                    return false;
                }
            }
            else if (!UserName.Equals(other.UserName))
            {
                return false;
            }

            return true;
        }
    }
}
