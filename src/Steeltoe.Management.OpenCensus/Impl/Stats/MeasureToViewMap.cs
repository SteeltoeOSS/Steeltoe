using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Stats.Measurements;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    internal sealed class MeasureToViewMap
    {
        private object _lck = new object();
        private readonly IDictionary<String, IList<MutableViewData>> mutableMap = new Dictionary<String, IList<MutableViewData>>();

        private IDictionary<IViewName, IView> registeredViews = new Dictionary<IViewName, IView>();

        // TODO(songya): consider adding a Measure.Name class
        private IDictionary<string, IMeasure> registeredMeasures = new Dictionary<string, IMeasure>();

        // Cached set of exported views. It must be set to null whenever a view is registered or
        // unregistered.
        private volatile ISet<IView> exportedViews;

        /** Returns a {@link ViewData} corresponding to the given {@link View.Name}. */
        internal IViewData GetView(IViewName viewName, IClock clock, StatsCollectionState state)
        {
            lock (_lck)
            {
                MutableViewData view = GetMutableViewData(viewName);
                return view == null ? null : view.ToViewData(clock.Now, state);
            }
        }

        internal ISet<IView> ExportedViews
        {
            get {
                ISet<IView> views = exportedViews;
                if (views == null)
                {
                    lock (_lck) {
                        exportedViews = views = FilterExportedViews(registeredViews.Values);
                    }
                }
                return views;
            }
        }

        // Returns the subset of the given views that should be exported
        private static ISet<IView> FilterExportedViews(ICollection<IView> allViews)
        {
            return ImmutableHashSet.CreateRange(allViews);
        }



        /** Enable stats collection for the given {@link View}. */
        internal void RegisterView(IView view, IClock clock)
        {
            lock (_lck)
            {
                exportedViews = null;
                registeredViews.TryGetValue(view.Name, out IView existing);
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
                IMeasure measure = view.Measure;
                registeredMeasures.TryGetValue(measure.Name, out IMeasure registeredMeasure);
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

        private void AddMutableViewData(string name, MutableViewData mutableViewData)
        {
            if (mutableMap.ContainsKey(name))
            {
                mutableMap[name].Add(mutableViewData);
            } else
            {
                mutableMap.Add(name, new List<MutableViewData>() { mutableViewData });
            }
        }

        private MutableViewData GetMutableViewData(IViewName viewName)
        {
            lock (_lck)
            {
                registeredViews.TryGetValue(viewName, out IView view);
                if (view == null)
                {
                    return null;
                }

                mutableMap.TryGetValue(view.Measure.Name, out IList<MutableViewData> views);
                if (views != null)
                {
                    foreach (MutableViewData viewData in views)
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

        // Records stats with a set of tags.
        internal void Record(ITagContext tags, IList<IMeasurement> stats, ITimestamp timestamp)
        {
            lock (_lck) {
                foreach (var measurement in stats) {
                    IMeasure measure = measurement.Measure;
                    registeredMeasures.TryGetValue(measure.Name, out IMeasure value);
                    if (!measure.Equals(value))
                    {
                        // unregistered measures will be ignored.
                        continue;
                    }
                    IList<MutableViewData> views = mutableMap[measure.Name];
                    foreach (MutableViewData view in views)
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
            lock (_lck)
            {
                foreach (var entry in mutableMap)
                {
                    foreach (MutableViewData mutableViewData in entry.Value)
                    {
                        mutableViewData.ClearStats();
                    }
                }
            }
        }

        // Resume stats collection for all MutableViewData.
        internal void ResumeStatsCollection(ITimestamp now)
        {
            lock (_lck)
            {
                foreach (var entry in mutableMap)
                {
                    foreach (MutableViewData mutableViewData in entry.Value)
                    {
                        mutableViewData.ResumeStatsCollection(now);
                    }
                }
            }
        }

    }
}
