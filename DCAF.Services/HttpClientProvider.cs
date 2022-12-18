using System.Net.Http;
using System.Threading.Tasks;
using TetraPak.XP;
using TetraPak.XP.Auth.Abstractions;
using TetraPak.XP.Web.Http;

namespace DCAF.Services
{
    sealed class HttpClientProvider : IHttpClientProvider
    {
        HttpClient? _httpClient;

        public Task<Outcome<HttpClient>> GetHttpClientAsync(HttpClientOptions? options = null)
        {
            if (_httpClient is { })
                return Task.FromResult(Outcome<HttpClient>.Success(_httpClient));

            _httpClient = new HttpClient();
            return Task.FromResult(Outcome<HttpClient>.Success(_httpClient));
        }

    }
}