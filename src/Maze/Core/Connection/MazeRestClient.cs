using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Maze.Client.Library.Services;
using Maze.Exceptions;
using Maze.Server.Connection;
using Maze.Server.Connection.Error;

namespace Maze.Core.Connection
{
    public class MazeRestClient : IMazeRestClient
    {
        private readonly HttpClient _httpClient;

        public MazeRestClient(HttpClient httpClient, Uri baseUri)
        {
            _httpClient = httpClient;
            BaseUri = baseUri;
        }

        public Uri BaseUri { get; }
        public string Jwt { get; private set; }

        public void SetAuthenticated(string jwt)
        {
            Jwt = jwt;
        }

        public async Task<HttpResponseMessage> SendMessage(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.RequestUri.IsAbsoluteUri)
                request.RequestUri = new Uri(BaseUri, request.RequestUri);

            if (Jwt != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Jwt);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
                return response;

            var result = await response.Content.ReadAsStringAsync();

            RestError[] errors;
            try
            {
                errors = JsonConvert.DeserializeObject<RestError[]>(result);
            }
            catch (Exception)
            {
                throw new HttpRequestException(result);
            }

            if (errors == null)
                response.EnsureSuccessStatusCode();

            var error = errors[0];
            switch (error.Type)
            {
                case ErrorTypes.ValidationError:
                case ErrorTypes.AuthenticationError:
                    throw new RestAuthenticationException(error);
                case ErrorTypes.NotFoundError:
                    throw new RestNotFoundException(error);
            }

            Debug.Fail($"The error type {error.Type} is not implemented.");
            throw new NotSupportedException(error.Message);
        }
    }
}
