using System.Threading;
using Discord.WebSocket;
using Nerva.Bots.Plugin;

namespace Fusion.Commands.Gaming
{
    [Command("draw", "Draw the lottery if I forget toi doi it myself")]
    public class DrawLottery : ICommand
    {
        private static readonly SemaphoreSlim commandLock = new SemaphoreSlim(1);

        public void Process(SocketUserMessage msg)
        {
            try{
                commandLock.WaitAsync();
            } catch { return; }
            
            try
            {
                LotteryManager.CurrentGame.DrawLottery(msg);
            }
            finally
            {
                commandLock.Release();
            }
        }
    }
}