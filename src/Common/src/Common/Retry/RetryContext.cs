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

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Common.Retry
{
    public class RetryContext : AbstractAttributeAccessor, IRetryContext
    {
        private const string LAST_EXCEPTION = "RetryContext.LastException";

        private const string RETRY_COUNT = "RetryContext.RetryCount";

        public Exception LastException
        {
            get
            {
                return (Exception)GetAttribute(LAST_EXCEPTION);
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                if (value == null && HasAttribute(LAST_EXCEPTION))
                {
                    RemoveAttribute(LAST_EXCEPTION);
                }
                else
                {
                    SetAttribute(LAST_EXCEPTION, value);
                }
            }
        }

        public int RetryCount
        {
            get
            {
                var result = (int?)GetAttribute(RETRY_COUNT);
                if (result == null)
                {
                    return 0;
                }

                return result.Value;
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                SetAttribute(RETRY_COUNT, value);
            }
        }
    }
}
