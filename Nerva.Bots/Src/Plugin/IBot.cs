using System.Threading.Tasks;
using AngryWasp.Cli.Args;

namespace Nerva.Bots.Plugin
{
    public interface IBot
    {
        void Init(Arguments args);

        Task ClientReady();

        IBotConfig Config { get; }
    }
}