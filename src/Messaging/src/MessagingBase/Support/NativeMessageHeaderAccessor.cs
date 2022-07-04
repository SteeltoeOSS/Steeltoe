// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Support;

public class NativeMessageHeaderAccessor : MessageHeaderAccessor
{
    public const string NativeHeaders = "nativeHeaders";

    private static readonly Dictionary<string, List<string>> Empty = new ();

    protected internal NativeMessageHeaderAccessor()
        : this((IDictionary<string, List<string>>)null)
    {
    }

    protected internal NativeMessageHeaderAccessor(IDictionary<string, List<string>> nativeHeaders)
    {
        if (nativeHeaders != null && nativeHeaders.Count > 0)
        {
            SetHeader(NativeHeaders, new Dictionary<string, List<string>>(nativeHeaders));
        }
    }

    protected internal NativeMessageHeaderAccessor(IMessage message)
        : base(message)
    {
        if (message != null)
        {
            var map = (IDictionary<string, List<string>>)GetHeader(NativeHeaders);
            if (map != null)
            {
                // Force removal since setHeader checks for equality
                RemoveHeader(NativeHeaders);
                SetHeader(NativeHeaders, new Dictionary<string, List<string>>(map));
            }
        }
    }

    public static string GetFirstNativeHeader(string headerName, IDictionary<string, object> headers)
    {
        headers.TryGetValue(NativeHeaders, out var obj);
        if (obj is IDictionary<string, List<string>> map)
        {
            map.TryGetValue(headerName, out var values);
            if (values != null)
            {
                return values[0];
            }
        }

        return null;
    }

    public string GetFirstNativeHeader(string headerName)
    {
        var map = GetNativeHeaders();
        if (map != null)
        {
            map.TryGetValue(headerName, out var values);
            if (values != null)
            {
                return values[0];
            }
        }

        return null;
    }

    public virtual IDictionary<string, List<string>> ToNativeHeaderDictionary()
    {
        var map = GetNativeHeaders();
        return map != null ? new Dictionary<string, List<string>>(map) : Empty;
    }

    public override void SetImmutable()
    {
        if (IsMutable)
        {
            var map = GetNativeHeaders();
            if (map != null)
            {
                // Force removal since setHeader checks for equality
                RemoveHeader(NativeHeaders);
                SetHeader(NativeHeaders, new Dictionary<string, List<string>>(map));
            }

            base.SetImmutable();
        }
    }

    public bool ContainsNativeHeader(string headerName)
    {
        var map = GetNativeHeaders();
        return map != null && map.ContainsKey(headerName);
    }

    public List<string> GetNativeHeader(string headerName)
    {
        var map = GetNativeHeaders();

        if (map != null)
        {
            map.TryGetValue(headerName, out var result);
            return result;
        }

        return null;
    }

    public void SetNativeHeader(string name, string value)
    {
        if (!IsMutable)
        {
            throw new InvalidOperationException("Already immutable");
        }

        var map = GetNativeHeaders();
        if (value == null)
        {
            if (map != null && map.TryGetValue(name, out _))
            {
                IsModified = true;
                map.Remove(name);
            }

            return;
        }

        if (map == null)
        {
            map = new Dictionary<string, List<string>>();
            SetHeader(NativeHeaders, map);
        }

        var values = new List<string> { value };
        if (!values.Equals(GetHeader(name)))
        {
            IsModified = true;
            map[name] = values;
        }
    }

    public void AddNativeHeader(string name, string value)
    {
        if (!IsMutable)
        {
            throw new InvalidOperationException("Already immutable");
        }

        if (value == null)
        {
            return;
        }

        var nativeHeaders = GetNativeHeaders();
        if (nativeHeaders == null)
        {
            nativeHeaders = new Dictionary<string, List<string>>();
            SetHeader(NativeHeaders, nativeHeaders);
        }

        if (!nativeHeaders.TryGetValue(name, out var values))
        {
            values = new List<string>();
            nativeHeaders.Add(name, values);
        }

        values.Add(value);
        IsModified = true;
    }

    public void AddNativeHeaders(IDictionary<string, List<string>> headers)
    {
        if (headers == null)
        {
            return;
        }

        foreach (var entry in headers)
        {
            var key = entry.Key;
            var values = entry.Value;
            foreach (var val in values)
            {
                AddNativeHeader(key, val);
            }
        }
    }

    public List<string> RemoveNativeHeader(string name)
    {
        if (!IsMutable)
        {
            throw new InvalidOperationException("Already immutable");
        }

        var nativeHeaders = GetNativeHeaders();
        if (nativeHeaders == null)
        {
            return null;
        }

        if (nativeHeaders.TryGetValue(name, out var existing))
        {
            nativeHeaders.Remove(name);
        }

        return existing;
    }

    protected virtual IDictionary<string, List<string>> GetNativeHeaders()
    {
        return (IDictionary<string, List<string>>)GetHeader(NativeHeaders);
    }
}
