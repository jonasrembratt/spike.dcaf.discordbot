using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DCAF.Discord
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DiscordService // injected service
    {
        readonly CancellationTokenSource _cts;
        readonly DiscordSocketClient _client;
        readonly TaskCompletionSource<bool> _clientStateTcs;

        public async Task<DiscordSocketClient> GetReadyClientAsync()
        {
            await _clientStateTcs.Task;
            return _client;
        }

        public async Task WhenReadyAsync(Action handler)
        {
            await _clientStateTcs.Task;
            handler.Invoke();
        }

        public void Exit()
        {
            if (_cts.Token.CanBeCanceled && !_cts.Token.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }
        
        public DiscordService(DiscordSocketClient client, CancellationTokenSource cts, TaskCompletionSource<bool> clientStateTcs)
        {
            _client = client;
            _cts = cts;
            _clientStateTcs = clientStateTcs;
            
        }

    }
}