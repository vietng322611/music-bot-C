using Newtonsoft.Json;
using MusicBot.Entities;
using System.IO;

namespace MusicBot.Services
{
    public class ConfigService
    {
        public Config GetConfig()
        {
            var file = "./Config.json";
            var data = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<Config>(data);
        }
    }
}
