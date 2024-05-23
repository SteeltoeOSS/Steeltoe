// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Steeltoe.Connectors.Services;

internal sealed class UriInfo
{
    private readonly char[] _questionMark =
    {
        '?'
    };

    private readonly char[] _colon =
    {
        ':'
    };

    public string Scheme { get; }

    public string Host { get; private set; }

    public string[] Hosts { get; private set; }

    public int Port { get; }

    public string UserName { get; }

    public string Password { get; }

    public string Path { get; }

    public string Query { get; }

    public string UriString { get; }

    public Uri Uri => MakeUri(UriString);

    public UriInfo(string scheme, string host, int port, string username, string password, string path = null, string query = null)
    {
        Scheme = scheme;
        Host = host;
        Port = port;
        UserName = WebUtility.UrlEncode(username);
        Password = WebUtility.UrlEncode(password);
        Path = path;
        Query = query;

        UriString = MakeUri(scheme, host, port, username, password, path, query).ToString();
    }

    public UriInfo(string uriString)
    {
        Uri uri = MakeUri(uriString);

        if (uri != null)
        {
            Scheme = uri.Scheme;
            Host ??= uri.Host;

            Port = uri.Port;
            Path = GetPath(uri.PathAndQuery);
            Query = GetQuery(uri.PathAndQuery);

            string[] userInfo = GetUserInfo(uri.UserInfo);
            UserName = userInfo[0];
            Password = userInfo[1];
        }

        UriString = uriString;
    }

    public UriInfo(string uriString, string username, string password)
    {
        Uri uri = MakeUri(uriString);

        if (uri != null)
        {
            Scheme = uri.Scheme;
            Host ??= uri.Host;

            Port = uri.Port;
            Path = GetPath(uri.PathAndQuery);
            Query = GetQuery(uri.PathAndQuery);
        }

        UserName = WebUtility.UrlEncode(username);
        Password = WebUtility.UrlEncode(password);
        UriString = uriString;
    }

    public override string ToString()
    {
        return UriString;
    }

    private Uri MakeUri(string scheme, string host, int port, string username, string password, string path, string query)
    {
        string cleanedPath = path == null || path.StartsWith('/') ? path : $"/{path}";
        cleanedPath = query != null ? $"{cleanedPath}?{query}" : cleanedPath;

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            var builder = new UriBuilder
            {
                Scheme = scheme,
                Host = host,
                Port = port,
                UserName = WebUtility.UrlEncode(username),
                Password = WebUtility.UrlEncode(password),
                Path = cleanedPath
            };

            return builder.Uri;
        }
        else
        {
            var builder = new UriBuilder
            {
                Scheme = scheme,
                Host = host,
                Port = port,
                Path = cleanedPath
            };

            return builder.Uri;
        }
    }

    private Uri MakeUri(string uriString)
    {
        if (uriString.StartsWith("jdbc", StringComparison.Ordinal) || uriString.Contains(';'))
        {
            uriString = ConvertJdbcToUri(uriString);
        }

        uriString = EscapeFirstAtSignIfMultipleOccurrences(uriString);

        try
        {
            return new Uri(uriString);
        }
        catch (UriFormatException)
        {
            // URI parsing will fail if multiple (comma separated) hosts were provided...
            if (uriString.Contains(','))
            {
                // Slide past the protocol
                string[] splitUri = uriString.Split('/');

                // get the host list (and maybe credentials)
                // -- pre-emptively set it as the Host property rather than a local variable
                //      since the connector is likely to expect this format here anyway
                string credentialAndHost = splitUri[2];

                // skip over credentials if they're present
                Host = credentialAndHost.Contains('@') ? credentialAndHost.Split('@')[1] : credentialAndHost;

                // add the hosts to a separate property for reconstruction later
                Hosts = Host.Split(',');

                // swap all the hosts out with a placeholder so we can parse the rest of the info
                return new Uri(uriString.Replace(Host, "multipleHostsDetected", StringComparison.Ordinal));
            }

            return null;
        }
    }

    private static string EscapeFirstAtSignIfMultipleOccurrences(string uriString)
    {
        // Workaround for CloudFoundry bug: it forgets to uri-escape the '@' character in username field when using Azure, resulting in an invalid Uri.
        // For example: postgresql://<user>@<host>:<password>@<host>:5432/vsbdb -> postgresql://<user>%40<host>:<password>@<host>:5432/vsbdb

        string[] parts = uriString.Split('@', 3);
        return parts.Length == 3 ? $"{parts[0]}%40{parts[1]}@{parts[2]}" : uriString;
    }

    private string ConvertJdbcToUri(string uriString)
    {
        uriString = uriString.Replace("jdbc:", string.Empty, StringComparison.Ordinal).Replace(';', '&');

        if (!uriString.Contains('?'))
        {
            int firstAmp = uriString.IndexOf('&');

            // If there is an equals sign before any ampersands, it is likely a key was included for the db name.
            // Make the database name part of the path rather than query string if possible
            int firstEquals = uriString.IndexOf('=');

            if (firstEquals > 0 && (firstEquals < firstAmp || firstAmp == -1))
            {
                int dbNameIndex = uriString.IndexOf("databasename=", StringComparison.OrdinalIgnoreCase);

                if (dbNameIndex > 0)
                {
                    uriString = uriString.Remove(dbNameIndex, 13);

                    // recalculate the location of the first '&'
                    firstAmp = uriString.IndexOf('&');
                }
            }

            if (firstAmp > 0)
            {
                uriString = uriString.Substring(0, firstAmp) + _questionMark[0] + uriString.Substring(firstAmp + 1, uriString.Length - firstAmp - 1);
            }
        }

        return uriString;
    }

    private string GetPath(string pathAndQuery)
    {
        if (string.IsNullOrEmpty(pathAndQuery))
        {
            return null;
        }

        string[] split = pathAndQuery.Split(_questionMark);

        if (split.Length == 0)
        {
            return null;
        }

        return split[0].Substring(1);
    }

    private string GetQuery(string pathAndQuery)
    {
        if (string.IsNullOrEmpty(pathAndQuery))
        {
            return null;
        }

        string[] split = pathAndQuery.Split(_questionMark);

        if (split.Length <= 1)
        {
            return null;
        }

        return split[1];
    }

    private string[] GetUserInfo(string userPass)
    {
        if (string.IsNullOrEmpty(userPass))
        {
            return new string[]
            {
                null,
                null
            };
        }

        string[] split = userPass.Split(_colon);

        if (split.Length != 2)
        {
            throw new ArgumentException($"Bad user/password in URI: {userPass}", nameof(userPass));
        }

        return split;
    }
}
