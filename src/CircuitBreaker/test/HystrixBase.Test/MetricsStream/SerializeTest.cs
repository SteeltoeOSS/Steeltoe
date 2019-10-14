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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class SerializeTest : HystrixTestBase
    {
        private class MyCommand : HystrixCommand<int>
        {
            public MyCommand()
                : base(
                    HystrixCommandGroupKeyDefault.AsKey("MyCommandGroup"),
                    () => { return 1; },
                    () => { return 2; })
            {
            }
        }

        [Fact]
        public void ToJsonList_ReturnsExpected()
        {
            var stream = HystrixDashboardStream.GetInstance();
            CountdownEvent latch = new CountdownEvent(1);

            List<string> result = null;
            var subscription = stream.Observe()
                .SubscribeOn(NewThreadScheduler.Default)
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(
                   (data) =>
                   {
                       result = Serialize.ToJsonList(data, null);
                       if (result.Count > 0)
                       {
                           latch.SignalEx();
                       }
                   },
                   (e) =>
                   {
                       latch.SignalEx();
                   },
                   () =>
                   {
                       latch.SignalEx();
                   });

            MyCommand cmd = new MyCommand();
            cmd.Execute();

            latch.Wait(10000);

            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            var jsonObject = result[0];

            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonObject);
            Assert.NotNull(dict);

            Assert.NotNull(dict["origin"]);
            Assert.NotNull(dict["data"]);
            JObject cmdData = (JObject)dict["data"];

            Assert.NotNull(cmdData["type"]);
            var type = cmdData["type"].Value<string>();
            Assert.True("HystrixCommand".Equals(type) || "HystrixThreadPool".Equals(type));
            Assert.NotNull(cmdData["name"]);
            var name = cmdData["name"].Value<string>();
            Assert.True("MyCommand".Equals(name) || "MyCommandGroup".Equals(name));

            subscription.Dispose();
        }
    }
}
