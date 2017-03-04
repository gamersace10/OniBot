﻿using OniBot.Interfaces;
using System;
using Discord;
using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using OniBot.Infrastructure;

namespace OniBot.Behaviors
{
    public class UpdateGameBehavior : IBotBehavior, IDisposable
    {
        public string Name => nameof(UpdateGameBehavior);

        private Timer _timer;
        private readonly BotConfig _config;
        private static Random _random;

        public UpdateGameBehavior(IOptions<BotConfig> config)
        {
            _config = config.Value;
            _random = new Random();
        }

        public async Task RunAsync(IDiscordClient botClient)
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
                _timer = null;
            }

            _timer = new Timer(UpdateGame, botClient, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(5));
            await Task.Yield();
        }

        private void UpdateGame(object state)
        {
            var client = state as DiscordSocketClient;
            if (client == null)
            {
                DiscordBot.Log(nameof(UpdateGame), LogSeverity.Error, $"client is a {state.GetType().Name}, and is not expected.");
                return;
            }
            
            try
            {
                var games = _config.Games;
                var index = _random.Next(0, games.Length - 1);
                var game = games[index];
                client.SetGameAsync(game).AsSync(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                //todo: log properly
                DiscordBot.Log(nameof(UpdateGame), LogSeverity.Critical, ex.ToString());
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}