// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http
{
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
            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PostAsync(url, content);
        }

        /// <summary>
        /// Convert an object to JSON and POST it
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="httpClient">HttpClient doing the sending</param>
        /// <param name="url">Url to POST to</param>
        /// <param name="data">Object to send</param>
        /// <param name="settings">Your Serializer Settings</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient httpClient, string url, T data, JsonSerializerSettings settings)
        {
            return PostAsJsonAsync(httpClient, new Uri(url), data, settings);
        }

        /// <summary>
        /// Convert an object to JSON and POST it
        /// </summary>
        /// <typeparam name="T">Type of object to serialize</typeparam>
        /// <param name="httpClient">HttpClient doing the sending</param>
        /// <param name="url">Url to POST to</param>
        /// <param name="data">Object to send</param>
        /// <param name="settings">Your Serializer Settings</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient httpClient, Uri url, T data, JsonSerializerSettings settings)
        {
            var dataAsString = JsonConvert.SerializeObject(data, settings);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PostAsync(url, content);
        }

        /// <summary>
        /// Convert an object to JSON and PUT it
        /// </summary>
        /// <typeparam name="T">the type of the data</typeparam>
        /// <param name="httpClient">provided HttpClient</param>
        /// <param name="url">the http endpoint to Put to</param>
        /// <param name="data">the data to put</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient httpClient, string url, T data)
        {
            return PutAsJsonAsync(httpClient, new Uri(url), data);
        }

        /// <summary>
        /// Convert an object to JSON and PUT it
        /// </summary>
        /// <typeparam name="T">the type of the data</typeparam>
        /// <param name="httpClient">provided HttpClient</param>
        /// <param name="url">the http endpoint to Put to</param>
        /// <param name="data">the data to put</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient httpClient, Uri url, T data)
        {
            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PutAsync(url, content);
        }

        /// <summary>
        /// Convert an object to JSON and PUT it
        /// </summary>
        /// <typeparam name="T">the type of the data</typeparam>
        /// <param name="httpClient">provided HttpClient</param>
        /// <param name="url">the http endpoint to Put to</param>
        /// <param name="data">the data to put</param>
        /// <param name="settings">the serialization setttings to use</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient httpClient, string url, T data, JsonSerializerSettings settings)
        {
            return PutAsJsonAsync(httpClient, new Uri(url), data, settings);
        }

        /// <summary>
        /// Convert an object to JSON and PUT it
        /// </summary>
        /// <typeparam name="T">the type of the data</typeparam>
        /// <param name="httpClient">provided HttpClient</param>
        /// <param name="url">the http endpoint to Put to</param>
        /// <param name="data">the data to put</param>
        /// <param name="settings">the serialization setttings to use</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient httpClient, Uri url, T data, JsonSerializerSettings settings)
        {
            var dataAsString = JsonConvert.SerializeObject(data, settings);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PutAsync(url, content);
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
            return JsonConvert.DeserializeObject<T>(dataAsString);
        }

        /// <summary>
        /// Convert JSON in HttpContent to a POCO
        /// </summary>
        /// <typeparam name="T">Type to deserialize into</typeparam>
        /// <param name="content">Content to be deserialized</param>
        /// <param name="settings">Your Serializer Settings</param>
        /// <returns>Your data, typed as your type</returns>
        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content, JsonSerializerSettings settings)
        {
            var dataAsString = await content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(dataAsString, settings);
        }
    }
}
