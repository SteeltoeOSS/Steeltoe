// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient
{
    /// <summary>
    ///  This is a version of Client extensions that uses System.Net.Json (instead of NewtonSoft)
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Convert an object to JSON and POST it
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="httpClient">HttpClient doing the sending</param>
        /// <param name="url">Url to POST to</param>
        /// <param name="data">Object to send</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient httpClient, string url, T data)
        {
            return PostAsJsonAsync(httpClient, new Uri(url), data);
        }

        /// <summary>
        /// Convert an object to JSON and POST it
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="httpClient">HttpClient doing the sending</param>
        /// <param name="url">Url to POST to</param>
        /// <param name="data">Object to send</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient httpClient, Uri url, T data)
        {
            var dataAsString = JsonSerializer.Serialize(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PostAsync(url, content);
        }

        /// <summary>
        /// Convert JSON in HttpContent to a POCO
        /// </summary>
        /// <typeparam name="T">Type to deserialize into</typeparam>
        /// <param name="content">Content to be deserialized</param>
        /// <returns>Your data, typed as your type</returns>
        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        {
            var dataAsString = await content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(dataAsString);
        }
    }
}