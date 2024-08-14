// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

#pragma warning disable SA1401 // Fields should be private

namespace Steeltoe.Common.Net;

internal static class NativeMethods
{
    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    internal static extern int WNetUseConnection(IntPtr hwndOwner, NetResource netResource, string? password, string? username, int flags, string? lpAccessName,
        string? lpBufferSize, string? lpResult);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    internal static extern int WNetCancelConnection2(string name, int flags, bool force);

    /// <summary>
    /// The NETRESOURCE structure contains information about a network resource. More info on NetResource:
    /// <seealso href="https://msdn.microsoft.com/en-us/c53d078e-188a-4371-bdb9-fc023bc0c1ba" />.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public sealed class NetResource
    {
        public ResourceScope Scope;
        public ResourceType ResourceType;
        public ResourceDisplayType DisplayType;
        public int Usage;
        public string? LocalName;
        public string? RemoteName;
        public string? Comment;
        public string? Provider;
    }

    /// <summary>
    /// The display options for the network object in a network browsing user interface.
    /// </summary>
    public enum ResourceDisplayType
    {
        Generic = 0x0,
        Domain = 0x01,
        Server = 0x02,
        Share = 0x03,
        File = 0x04,
        Group = 0x05,
        Network = 0x06,
        Root = 0x07,
        ShareAdmin = 0x08,
        Directory = 0x09,
        Tree = 0x0a,
        NdsContainer = 0x0b
    }

    /// <summary>
    /// Scope of the file share.
    /// </summary>
    public enum ResourceScope
    {
        Connected = 1,
        GlobalNetwork,
        Remembered,
        Recent,
        Context
    }

    /// <summary>
    /// Type of network resource.
    /// </summary>
    public enum ResourceType
    {
        Any = 0,
        Disk = 1,
        Print = 2,
        Reserved = 8
    }
}
