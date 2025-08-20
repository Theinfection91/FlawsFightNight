using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlawsFightNight.Managers
{
    public class LiveViewManager
    {
        public LiveViewManager()
        {
            Console.WriteLine("LiveViewManager initialized.");
            StartMatchesLiveViewTask();
        }

        public void StartMatchesLiveViewTask()
        {
            Task.Run(() => RunMatchesUpdateTaskAsync());
        }

        private async Task RunMatchesUpdateTaskAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(11));
                await SendMatchesToChannelAsync();
            }
        }

        private async Task SendMatchesToChannelAsync()
        {
            // Placeholder for sending match updates to a Discord channel
            Console.WriteLine($"{DateTime.Now} - Sending match updates to channel...");
            await Task.CompletedTask;
        }
    }
}
