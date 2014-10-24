using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using Windows.Web.Http.Headers;

namespace Discouser.Data
{

    public partial class ApiConnection
    {

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
                var result = await _client.SendRequestAsync(BuildRequest(_host, path, method, parameters));

                if (!result.IsSuccessStatusCode && (await result.Content.ReadAsStringAsync()) == "['BAD CSRF']")
                {
                    _client.DefaultRequestHeaders.Append("X-CSRF-Token", await GetCsrfToken());
                    result = await _client.SendRequestAsync(BuildRequest(_host, path, method, parameters));
                }
                if (!result.IsSuccessStatusCode)
                {
                    await _logger.Log(result);
                    return null;
                }

                return await Deserialize(result);
            }
            catch (Exception é)
            {
                await _logger.Log(é, "HttpRequest to " + path + "failed.");
                return null;
            }
        }

        private async Task<string> GetCsrfToken()
        {
            var uriString = _host + "/session/csrf.json";
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
                catch (Exception é)
                {
                    await _logger.Log(é, "Deserialize failed.");
                    return null;
                }
            }
        }

        private static HttpRequestMessage BuildRequest(string host, string path, HttpMethod method, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var sendContent = !(method == HttpMethod.Put || method == HttpMethod.Post || method == HttpMethod.Patch);
            
            var query = "?";
            if (!sendContent)
            {
                path = (path.StartsWith("/") ? "" : "/") + path + (path.EndsWith("/") ? "" : "/");
                foreach (var kv in parameters)
                {
                    query += Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value) + "&";
                }
                if (!string.IsNullOrEmpty(query))
                {
                    query = query.Substring(0, query.Length - 1);
                }
            }

            if (!string.IsNullOrEmpty(path))
            {
                if (path.Contains('?'))
                {
                    query = query.Replace('?', '&');
                }
                else
                {
                    path = path.Substring(0, path.Length - 1) + ".json";
                }
            }

            var uri = new Uri(host + path + (sendContent ? "" : query));
            var request = new HttpRequestMessage(method, uri);
            request.Headers.Connection.TryParseAdd("Keep-Alive");

            if (sendContent)
            {
                request.Content = new HttpFormUrlEncodedContent(parameters);
            }
            
            return request;
        }
    }
}