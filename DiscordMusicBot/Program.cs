using System.Threading.Tasks;

namespace MusicBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new BotClient().InitializeAsync();
        }
    }
}
