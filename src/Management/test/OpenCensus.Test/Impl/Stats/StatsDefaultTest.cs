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

namespace Steeltoe.Management.Census.Stats.Test
{
    [Obsolete]
    public class StatsDefaultTest
    {
        //// [Fact]
        //// public void loadStatsManager_UsesProvidedClassLoader()
        ////      {
        ////          final RuntimeException toThrow = new RuntimeException("UseClassLoader");
        ////          thrown.expect(RuntimeException.class);
        ////  thrown.expectMessage("UseClassLoader");
        ////  Stats.loadStatsComponent(
        ////      new ClassLoader()
        ////      {
        ////          @Override
        ////        public Class<?> loadClass(String name)
        ////          {
        ////              throw toThrow;
        ////          }
        ////      });
        //// }

        //// [Fact]
        ////  public void loadStatsManager_IgnoresMissingClasses()
        ////    {
        ////        ClassLoader classLoader =
        ////            new ClassLoader() {
        ////          @Override
        ////              public Class<?> loadClass(String name) throws ClassNotFoundException {
        ////            throw new ClassNotFoundException();
        ////        }
        ////    };

        //// assertThat(Stats.loadStatsComponent(classLoader).getClass().getName())
        ////        .isEqualTo("io.opencensus.stats.NoopStats$NoopStatsComponent");
        //// }

        [Fact]
        public void DefaultValues()
        {
            Assert.Equal(NoopStats.NoopStatsRecorder, Stats.StatsRecorder);
            Assert.Equal(NoopStats.NewNoopViewManager().GetType(), Stats.ViewManager.GetType());
        }

        [Fact]
        public void GetState()
        {
            Assert.Equal(StatsCollectionState.DISABLED, Stats.State);
        }
    }
}
