using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using TetraPak.XP;

namespace DCAF.Discord
{
    public class DiscordService 
    {
        readonly CancellationTokenSource _cts;
        readonly DiscordSocketClient _client;
        readonly TaskCompletionSource<bool> _clientReadyTcs = new();

        public async Task<DiscordSocketClient> GetReadyClientAsync()
        {
            await _clientReadyTcs.Task;
            return _client;
        }

        public void SetReady()
        {
            if (!_clientReadyTcs.IsFinished())
            {
                _clientReadyTcs.SetResult(true);
            }
        }

        public void Exit()
        {
            if (_cts.Token.CanBeCanceled && !_cts.Token.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }
        
        public DiscordService(DiscordSocketClient client, CancellationTokenSource cts)
        {
            _cts = cts;
            _client = client;
        }
    }
}