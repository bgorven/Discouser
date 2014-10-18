using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Discouser.Api
{

    public partial class ApiConnection
    {
        /// <summary>
        /// Tuple.Create for kvp
        /// </summary>
        public static KeyValuePair<TName, TValue> Parameter<TName, TValue>(TName name, TValue value)
        {
            return new KeyValuePair<TName, TValue>(name, value);
        }

        public Task<JToken> Get(string path, params KeyValuePair<string, string>[] parameters)
        {
            return Request(path, HttpMethod.Get, parameters);
        }

        public Task<JToken> Post(string path, params KeyValuePair<string, string>[] parameters)
        {
            return Request(path, HttpMethod.Post, parameters);
        }

        private async Task<JToken> Request(string path, HttpMethod method, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            try
            {
                var result = await _client.SendRequestAsync(BuildRequest(path, method, parameters));

                if (!result.IsSuccessStatusCode && (await result.Content.ReadAsStringAsync()) == "['BAD CSRF']")
                {
                    _client.DefaultRequestHeaders.Append("X-CSRF-Token", await GetCsrfToken());
                    result = await _client.SendRequestAsync(BuildRequest(path, method, parameters));
                }
                if (!result.IsSuccessStatusCode)
                {
                    await _logger.Log(result);
                    return null;
                }

                return await Deserialize(result);
            }
            catch (Exception ex)
            {
                await _logger.Log(ex, "HttpRequest to " + path + "failed.");
                return null;
            }
        }

        private async Task<string> GetCsrfToken()
        {
            var uriString = _host + ((_host.Last() == '/') ? "session/csrf.json" : "/session/csrf.json");
            var csrfSource = new Uri(uriString);
            var csrfResult = _client.GetAsync(csrfSource);
            var csrfToken = (await Deserialize(await csrfResult))["csrf"].ToString();
            return csrfToken;
        }

        private async Task<JToken> Deserialize(HttpResponseMessage result)
        {

            using (var inputStream = await result.Content.ReadAsInputStreamAsync())
            using (var streamReader = new StreamReader(inputStream.AsStreamForRead()))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                try
                {
                    return JToken.ReadFrom(jsonTextReader);
                }
                catch (Exception ex)
                {
                    await _logger.Log(ex, "Deserialize failed.");
                    return null;
                }
            }
        }

        private static HttpRequestMessage BuildRequest(string path, HttpMethod method, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var query = "";
            path = (path.StartsWith("/") ? "" : "/") + path + (path.EndsWith("/") ? "" : "/");

            foreach (var kv in parameters)
            {
                var replacement = path.Replace("/" + kv.Key + "/", "/" + kv.Value + "/");
                if (replacement != path)
                {
                    path = replacement;
                }
                else
                {
                    query += Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value) + "&";
                }
            }

            path = path.Substring(0, path.Length - 1) + ".json";
            query = query.Substring(0, query.Length - 1);

            var uri = new Uri(path + "?" + query);

            var request = new HttpRequestMessage(method, uri);
            if (method == HttpMethod.Put || method == HttpMethod.Post || method == HttpMethod.Patch)
            {
                request.Content = new HttpFormUrlEncodedContent(parameters);
            }

            return request;
        }
    }
}