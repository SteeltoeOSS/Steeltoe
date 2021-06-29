// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s.Models;
using System.Threading.Tasks;

namespace Steeltoe.Common.Kubernetes
{
    public interface IPodUtilities
    {
        Task<V1Pod> GetCurrentPodAsync();
    }
}
