// <copyright file="MeasureToViewMap.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Stats
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using OpenCensus.Common;
    using OpenCensus.Tags;

    internal sealed class MeasureToViewMap
    {
        private readonly object lck = new object();
        private readonly IDictionary<string, IList<MutableViewData>> mutableMap = new Dictionary<string, IList<MutableViewData>>();

        private readonly IDictionary<IViewName, IView> registeredViews = new Dictionary<IViewName, IView>();

        // TODO(songya): consider adding a Measure.Name class
        private readonly IDictionary<string, IMeasure> registeredMeasures = new Dictionary<string, IMeasure>();

        // Cached set of exported views. It must be set to null whenever a view is registered or
        // unregistered.
        private volatile ISet<IView> exportedViews;

        /// <summary>
        /// Gets a {@link ViewData} corresponding to the given {@link View.Name}.
        /// </summary>
        internal ISet<IView> ExportedViews
        {
            get
            {
                var views = exportedViews;
                if (views == null)
                {
                    lock (lck)
                    {
                        exportedViews = views = FilterExportedViews(registeredViews.Values);
                    }
                }

                return views;
            }
        }

        internal IViewData GetView(IViewName viewName, IClock clock, StatsCollectionState state)
        {
            lock (lck)
            {
                var view = GetMutableViewData(viewName);
                return view?.ToViewData(clock.Now, state);
            }
        }

        /** Enable stats collection for the given {@link View}. */
        internal void RegisterView(IView view, IClock clock)
        {
            lock (lck)
            {
                exportedViews = null;
                registeredViews.TryGetValue(view.Name, out var existing);
                if (existing != null)
                {
                    if (existing.Equals(view))
                    {
                        // Ignore views that are already registered.
                        return;
                    }
                    else
                    {
                        throw new ArgumentException("A different view with the same name is already registered: " + existing);
                    }
                }

                var measure = view.Measure;
                registeredMeasures.TryGetValue(measure.Name, out var registeredMeasure);
                if (registeredMeasure != null && !registeredMeasure.Equals(measure))
                {
                    throw new ArgumentException("A different measure with the same name is already registered: " + registeredMeasure);
                }

                registeredViews.Add(view.Name, view);
                if (registeredMeasure == null)
                {
                    registeredMeasures.Add(measure.Name, measure);
                }

                AddMutableViewData(view.Measure.Name, MutableViewData.Create(view, clock.Now));
            }
        }

        // Records stats with a set of tags.
        internal void Record(ITagContext tags, IEnumerable<IMeasurement> stats, ITimestamp timestamp)
        {
            lock (lck)
            {
                foreach (var measurement in stats)
                {
                    var measure = measurement.Measure;
                    registeredMeasures.TryGetValue(measure.Name, out var value);
                    if (!measure.Equals(value))
                    {
                        // unregistered measures will be ignored.
                        continue;
                    }

                    var views = mutableMap[measure.Name];
                    foreach (var view in views)
                    {
                        measurement.Match<object>(
                            (arg) =>
                            {
                                view.Record(tags, arg.Value, timestamp);
                                return null;
                            },
                            (arg) =>
                            {
                                view.Record(tags, arg.Value, timestamp);
                                return null;
                            },
                            (arg) =>
                            {
                                throw new ArgumentException();
                            });
                    }
                }
            }
        }

        // Clear stats for all the current MutableViewData
        internal void ClearStats()
        {
            lock (lck)
            {
                foreach (var entry in mutableMap)
                {
                    foreach (var mutableViewData in entry.Value)
                    {
                        mutableViewData.ClearStats();
                    }
                }
            }
        }

        // Resume stats collection for all MutableViewData.
        internal void ResumeStatsCollection(ITimestamp now)
        {
            lock (lck)
            {
                foreach (var entry in mutableMap)
                {
                    foreach (var mutableViewData in entry.Value)
                    {
                        mutableViewData.ResumeStatsCollection(now);
                    }
                }
            }
        }

        // Returns the subset of the given views that should be exported.
        private static ISet<IView> FilterExportedViews(ICollection<IView> allViews)
        {
            return ImmutableHashSet.CreateRange(allViews);
        }

        private void AddMutableViewData(string name, MutableViewData mutableViewData)
        {
            if (mutableMap.ContainsKey(name))
            {
                mutableMap[name].Add(mutableViewData);
            }
            else
            {
                mutableMap.Add(name, new List<MutableViewData>() { mutableViewData });
            }
        }

        private MutableViewData GetMutableViewData(IViewName viewName)
        {
            lock (lck)
            {
                registeredViews.TryGetValue(viewName, out var view);
                if (view == null)
                {
                    return null;
                }

                mutableMap.TryGetValue(view.Measure.Name, out var views);
                if (views != null)
                {
                    foreach (var viewData in views)
                    {
                        if (viewData.View.Name.Equals(viewName))
                        {
                            return viewData;
                        }
                    }
                }

                throw new InvalidOperationException(
                    "Internal error: Not recording stats for view: \""
                        + viewName
                        + "\" registeredViews="
                        + registeredViews
                        + ", mutableMap="
                        + mutableMap);
            }
        }
    }
}
