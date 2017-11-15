// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
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
        /// <param name="settings">Your Serializer Settings</param>
        /// <param name="url">Url to POST to</param>
        /// <param name="data">Object to send</param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient httpClient, string url, T data, JsonSerializerSettings settings)
        {
            var dataAsString = JsonConvert.SerializeObject(data, settings);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PostAsync(url, content);
        }

        /// <summary>
        /// Convert an object to JSON and PUT it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpClient"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient httpClient, string url, T data)
        {
            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PutAsync(url, content);
        }

        /// <summary>
        /// Convert an object to JSON and PUT it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpClient"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns>Task to be awaited</returns>
        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient httpClient, string url, T data, JsonSerializerSettings settings)
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
            var dataAsString = await content.ReadAsStringAsync();
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
            var dataAsString = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(dataAsString, settings);
        }
    }
}
