// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.OpenTelemetry.Metrics
{
    public static class ViewsExtensions
    {
        public static MeterProviderBuilder AddRegisteredViews(this MeterProviderBuilder builder, IViewRegistry viewRegistry)
        {
            if (viewRegistry != null)
            {
                foreach (var view in viewRegistry.Views)
                {
                    builder.AddView(view.Key, view.Value);
                }
            }

            return builder;
        }
    }
}
