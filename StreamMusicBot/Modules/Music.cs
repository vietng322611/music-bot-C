using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using StreamMusicBot.Services;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

namespace StreamMusicBot.Modules
{
    public class Music : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client;
        private readonly LogService _logService;
        private readonly LavaRestClient _lavaRestClient;
        private readonly LavaSocketClient _lavaSocketClient;
        public Music(LavaRestClient lavaRestClient, DiscordSocketClient client,
            LavaSocketClient lavaSocketClient, LogService logService)
        {
            _client = client;
            _lavaRestClient = lavaRestClient;
            _lavaSocketClient = lavaSocketClient;
            _logService = logService;
        }
        public Task InitializeAsync()
        {
            _client.Ready += ClientReadyAsync;
            _lavaSocketClient.Log += LogAsync;
            _lavaSocketClient.OnTrackFinished += TrackFinished;
            return Task.CompletedTask;
        }
        private async Task ClientReadyAsync()
        {
            await _lavaSocketClient.StartAsync(_client);
        }

        private async Task TrackFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
        {
            if (!reason.ShouldPlayNext())
                return;

            if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTrack))
            {
                await player.TextChannel.SendMessageAsync("There are no more tracks in the queue.");
                return;
            }

            await player.PlayAsync(nextTrack);
        }

        private async Task LogAsync(LogMessage logMessage)
        {
            await _logService.LogAsync(logMessage);
        }
        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel textChannel)
            => await _lavaSocketClient.ConnectAsync(voiceChannel, textChannel);

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
            => await _lavaSocketClient.DisconnectAsync(voiceChannel);

        [Command("play")]
        public async Task Play([Remainder] string query)
        {
            var user = Context.User as SocketGuildUser;
            var player = _lavaSocketClient.GetPlayer(Context.Guild.Id);
            var results = await _lavaRestClient.SearchYouTubeAsync(query);
            if (user.VoiceChannel is null)
            {
                await ReplyAsync("You need to connect to a voice channel.");
                return;
            }
            else
            {
                await ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
            }


            if (results.LoadType == LoadType.NoMatches || results.LoadType == LoadType.LoadFailed)
            {
                await ReplyAsync("No matches found.");
            }

            var track = results.Tracks.FirstOrDefault();

            if (player.IsPlaying)
            {
                player.Queue.Enqueue(track);
                await ReplyAsync($"{track.Title} has been added to the queue.");
            }
            else
            {
                await player.PlayAsync(track);
                await ReplyAsync($"Now Playing: {track.Title}");
            }

        }

        [Command("stop")]
        public async Task Stop()
        {
            var user = Context.User as SocketGuildUser;
            var player = _lavaSocketClient.GetPlayer(Context.Guild.Id);
            if (user.VoiceChannel is null)
            {
                await ReplyAsync("I'm not playing anything.");
            }
            else
            {
                if (player.IsPlaying) 
                {
                    await player.StopAsync();
                }
                else 
                {
                    await LeaveAsync(user.VoiceChannel);
                    await ReplyAsync("Player stopped.");
                }
                
            }
        }

        [Command("skip")]
        public async Task Skip()
        {
            var player = _lavaSocketClient.GetPlayer(Context.Guild.Id);
            if (player is null || player.Queue.Items.Count() is 0)
                await ReplyAsync("No song left.");
            else
            {
                var oldTrack = player.CurrentTrack;
                await player.SkipAsync();
                await ReplyAsync($"Skiped: {oldTrack.Title} \nNow Playing: {player.CurrentTrack.Title}");
            }
        }

        [Command("volume")]
        public async Task Volume(int vol)
        {
            var player = _lavaSocketClient.GetPlayer(Context.Guild.Id);
            if (player is null)
                await ReplyAsync("Player isn't playing.");

            if (vol > 150 || vol <= 0)
            {
                await ReplyAsync("Please use a number between 0 - 150");
            }

            await player.SetVolumeAsync(vol);
            await ReplyAsync($"**Volume set to**: {vol}/150");
        }

        [Command("pause")]
        public async Task Pause()
        {
            var player = _lavaSocketClient.GetPlayer(Context.Guild.Id);
            if (player is null)
                await ReplyAsync("Player isn't playing.");

            if (!player.IsPaused)
            {
                await player.PauseAsync();
                await ReplyAsync("Player is Paused.");
            }
            else
            {
                await player.ResumeAsync();
                await ReplyAsync("Playback resumed.");
            }
        }

        [Command("resume")]
        public async Task Resume()
        {
            var player = _lavaSocketClient.GetPlayer(Context.Guild.Id);
            if (player is null)
                await ReplyAsync("I'm not playing anything.");

            if (player.IsPaused)
            {
                await player.ResumeAsync();
                await ReplyAsync("Player resumed.");
            }
            await ReplyAsync("Player is not paused.");
        }
    }
}
