﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OniBot.Infrastructure;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OniBot.Interfaces
{
    class SocketDiscordBot : IDiscordBot, IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly ICommandHandler _commandHandler;
        private readonly IApplicationLifetime _appLifetime;
        private readonly Dictionary<string, IBotBehavior> _behaviors;
        private readonly IBehaviorService _behaviorService;
        private readonly ILogger<SocketDiscordBot> _logger;
        private readonly BotConfig _configuration;

        public SocketDiscordBot(BotConfig config, DiscordSocketClient client, IBehaviorService behaviorService, ICommandHandler commandHandler, ILogger<SocketDiscordBot> logger, IConfiguration configroot, IApplicationLifetime appLifetime)
        {
            _configuration = config;
            _behaviorService = behaviorService;
            _logger = logger;
            _behaviors = new Dictionary<string, IBotBehavior>();
            _client = client as DiscordSocketClient;
            _commandHandler = commandHandler;
            _appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return RunBotAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.SetStatusAsync(UserStatus.Offline);
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        public async Task RunBotAsync()
        {
            var logger = new DiscordLogger<SocketDiscordBot>(_logger);
            _client.Ready += OnReadyAsync;
            _client.Log += logger.OnLogAsync;
            _client.LoggedOut += OnLoggedOutAsync;
            _client.Disconnected += OnDisconnected;

            await _behaviorService.InstallAsync();

            try
            {
                await ConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        private Task OnDisconnected(Exception arg)
        {
            _logger.LogError(arg);
            _appLifetime.StopApplication();
            return Task.CompletedTask;
        }

        private async Task OnLoggedOutAsync()
        {
            await _behaviorService.StopAsync();
        }

        private async Task OnReadyAsync()
        {
            await _behaviorService.RunAsync();
            await _commandHandler.InstallAsync();
        }

        private async Task ConnectAsync()
        {
            var maxAttempts = 10;
            var currentAttempt = 0;
            do
            {
                currentAttempt++;
                try
                {
                    await _client.LoginAsync(TokenType.Bot, _configuration.Token);
                    await _client.StartAsync();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Fialed to connect: {ex.Message}");
                    await Task.Delay(currentAttempt * 1000);
                }
            }
            while (currentAttempt < maxAttempts);
        }
    }
}
