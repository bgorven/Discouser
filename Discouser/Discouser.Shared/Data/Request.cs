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
        public Task<JToken> Get(string path, string[] jsonPath, params KeyValuePair<string, string>[] parameters)
        {
            return Request(path, HttpMethod.Get, parameters, jsonPath);
        }

        public Task<JToken> Post(string path, string[] jsonPath, params KeyValuePair<string, string>[] parameters)
        {
            return Request(path, HttpMethod.Post, parameters, jsonPath);
        }

        private async Task<JToken> Request(string relativePath, 
            HttpMethod method, 
            IEnumerable<KeyValuePair<string, string>> parameters,
            string[] jsonPath)
        {
            var request = BuildRequest(_host, relativePath, method, parameters);

            try
            {
                var result = await _client.SendRequestAsync(request);

                if (!result.IsSuccessStatusCode && (await result.Content.ReadAsStringAsync()) == "['BAD CSRF']")
                {
                    _client.DefaultRequestHeaders.Append("X-CSRF-Token", await GetCsrfToken());

                    request = BuildRequest(_host, relativePath, method, parameters);
                    result = await _client.SendRequestAsync(request);
                }
                if (!result.IsSuccessStatusCode)
                {
                    await _logger.Log(result);
                    return null;
                }

                return await Deserialize(result, jsonPath);
            }
            catch (Exception é)
            {
                var task = _logger.Log(é, "HttpRequest to " + relativePath + "failed.");
                return null;
            }
        }

        private static readonly string[] _csrfPath = new string[] { "csrf" };
        private async Task<string> GetCsrfToken()
        {
            var uriString = _host + "/session/csrf.json";
            var csrfSource = new Uri(uriString);
            var csrfResult = _client.GetAsync(csrfSource);
            var csrfToken = (string)await Deserialize(await csrfResult, _csrfPath);
            return csrfToken;
        }

        private async Task<JToken> Deserialize(HttpResponseMessage message, string[] path)
        {
            path = path ?? new string[0];

            using (var inputStream = await message.Content.ReadAsInputStreamAsync())
            using (var streamReader = new StreamReader(inputStream.AsStreamForRead()))
            using (var reader = new JsonTextReader(streamReader))
            {
                try
                {
                    if (!reader.Read()) throw new ArgumentException("Response missing any JSON data", "result");
                    if (reader.TokenType != JsonToken.StartObject) throw new InvalidDataException("Can only browse JSON objects.");
                    
                    for (var index = 0; index < path.Length; index++)
                    {

                        //when this loop ends, the start of the desired object will be the current token in the reader.
                        while (true)
                        {
                            if (!reader.Read()) throw new InvalidDataException("Malformed JSON data.");
                            if (reader.TokenType != JsonToken.PropertyName) throw new InvalidDataException(
                                "Property “" + string.Join(".", path, 0, index + 1) + "” not found.");

                            if (path[index].Equals(reader.Value))
                            {
                                reader.Read();
                                break;
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }
                    }

                    return JToken.ReadFrom(reader);
                }
                catch (Exception é)
                {
                    var task = _logger.Log(é, "Deserialize failed.");
                    return null;
                }
            }
        }

        private static HttpRequestMessage BuildRequest(string host, string path, HttpMethod method, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var sendContent = method == HttpMethod.Put || method == HttpMethod.Post || method == HttpMethod.Patch;
            path = (path.StartsWith("/") ? "" : "/") + path + (path.EndsWith("/") ? "" : "/");
            
            var query = "?";
            if (!sendContent)
            {
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