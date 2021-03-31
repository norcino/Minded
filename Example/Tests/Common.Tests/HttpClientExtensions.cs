using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Common.Tests
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PostAsync<T>(this HttpClient httpClient,
            string url, T entity)
        {
            var content = new StringContent(JsonConvert.SerializeObject(entity), Encoding.UTF8, "application/json");
            return await httpClient.PostAsync(url, content);
        }
    }
}
