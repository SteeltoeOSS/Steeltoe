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

using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class AttributeValueTest
    {
        [Fact]
        public void StringAttributeValue()
        {
            IAttributeValue<string> attribute = AttributeValue<string>.Create("MyStringAttributeValue");
            attribute.Apply<object>((stringValue) =>
            {
                Assert.Equal("MyStringAttributeValue", stringValue);
                return null;
            });
        }

        [Fact]
        public void BooleanAttributeValue()
        {
            IAttributeValue<bool> attribute = AttributeValue<bool>.Create(true);
            attribute.Apply<object>((boolValue) =>
            {
                Assert.True(boolValue);
                return null;
            });
        }

        [Fact]
        public void LongAttributeValue()
        {
            IAttributeValue<long> attribute = AttributeValue<long>.Create(123456L);
            attribute.Apply<object>((longValue) =>
            {
                Assert.Equal(123456L, longValue);
                return null;
            });
        }

        [Fact]
        public void AttributeValue_EqualsAndHashCode()
        {
            // EqualsTester tester = new EqualsTester();
            // tester.addEqualityGroup(
            //    AttributeValue.stringAttributeValue("MyStringAttributeValue"),
            //    AttributeValue.stringAttributeValue("MyStringAttributeValue"));
            // tester.addEqualityGroup(AttributeValue.stringAttributeValue("MyStringAttributeDiffValue"));
            // tester.addEqualityGroup(
            //    AttributeValue.booleanAttributeValue(true), AttributeValue.booleanAttributeValue(true));
            // tester.addEqualityGroup(AttributeValue.booleanAttributeValue(false));
            // tester.addEqualityGroup(
            //    AttributeValue.longAttributeValue(123456L), AttributeValue.longAttributeValue(123456L));
            // tester.addEqualityGroup(AttributeValue.longAttributeValue(1234567L));
            // tester.testEquals();
        }

        [Fact]
        public void AttributeValue_ToString()
        {
            IAttributeValue<string> attribute = AttributeValue<string>.Create("MyStringAttributeValue");
            Assert.Contains("MyStringAttributeValue", attribute.ToString());
            IAttributeValue<bool> attribute2 = AttributeValue<bool>.Create(true);
            Assert.Contains("True", attribute2.ToString());
            IAttributeValue<long> attribute3 = AttributeValue<long>.Create(123456L);
            Assert.Contains("123456", attribute3.ToString());
        }
    }
}
