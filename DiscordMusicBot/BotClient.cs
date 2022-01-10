using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using Victoria;
using MusicBot.Modules;
using MusicBot.Services;
using MusicBot.Entities;

namespace MusicBot
{
    public class BotClient
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _cmdService;
        private readonly LogService _logService;
        private readonly ConfigService _configService;
        private readonly Config _config;
        private readonly LavaSocketClient _lavaSocketClient;
        private Configuration _victoriaConfig;
        private IServiceProvider _services;

        public BotClient()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                MessageCacheSize = 50,
                LogLevel = LogSeverity.Debug
            });

            _cmdService = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false
            });

            _lavaSocketClient = new LavaSocketClient();

            _logService = new LogService();
            _configService = new ConfigService();
            _config = _configService.GetConfig();
            _victoriaConfig = new Configuration
            {
                Host = "LocalHost",
                Password = "youshallnotpass"
            };
        }

        public async Task InitializeAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();
            _lavaSocketClient.Log += LogAsync;
            _services = SetupServices();

            var cmdHandler = new CommandHandler(_client, _cmdService, _services);
            await cmdHandler.InitializeAsync();

            await _services.GetRequiredService<Music>().InitializeAsync();
            await Task.Delay(-1);
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await _logService.LogAsync(logMessage);
        }

        private IServiceProvider SetupServices()
            => new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_cmdService)
            .AddSingleton(_logService)
            .AddSingleton(new LavaRestClient(_victoriaConfig))
            .AddSingleton<Music>()
            .AddSingleton<LavaSocketClient>()
            .BuildServiceProvider();
    }
}