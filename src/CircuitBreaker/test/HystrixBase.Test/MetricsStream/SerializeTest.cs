// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                    () => 1,
                    () => 2)
            {
            }
        }

        [Fact]
        public void ToJsonList_ReturnsExpected()
        {
            var stream = HystrixDashboardStream.GetInstance();
            var latch = new CountdownEvent(1);

            List<string> result = null;
            var subscription = stream.Observe()
                .SubscribeOn(NewThreadScheduler.Default)
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(
                   data =>
                   {
                       result = Serialize.ToJsonList(data, null);
                       if (result.Count > 0)
                       {
                           latch.SignalEx();
                       }
                   },
                   e =>
                   {
                       latch.SignalEx();
                   },
                   () =>
                   {
                       latch.SignalEx();
                   });

            var cmd = new MyCommand();
            cmd.Execute();

            latch.Wait(10000);

            Assert.NotNull(result);
            Assert.True(result.Count > 0);

            var jsonObject = result[0];

            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonObject);
            Assert.NotNull(dict);

            Assert.NotNull(dict["origin"]);
            Assert.NotNull(dict["data"]);
            var cmdData = (JObject)dict["data"];

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
