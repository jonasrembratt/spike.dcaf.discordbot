using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TetraPak.XP;
using TetraPak.XP.Auth.Abstractions;
using TetraPak.XP.Web.Http;

namespace DCAF.Services
{
    class HttpClientProvider : IHttpClientProvider
    {
        HttpClient? _httpClient;

        public Task<Outcome<HttpClient>> GetHttpClientAsync(
            SecureClientOptions? options = null, 
            CancellationToken? cancellationToken = null)
        {
            if (_httpClient is { })
                return Task.FromResult(Outcome<HttpClient>.Success(_httpClient));

            _httpClient = new HttpClient();
            return Task.FromResult(Outcome<HttpClient>.Success(_httpClient));
        }
    }
}