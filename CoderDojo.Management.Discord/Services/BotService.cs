using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Discord;
using Discord.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;
using CoderDojo.Management.Discord.InteractionServices;
using CoderDojo.Management.Discord.Services.Interaction;
//using CoderDojo.Management.Discord.Services.Challenge;

internal class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly CommandHandlingService _commandHandlingService;
    private readonly SlashCommandService _slashCommandService;
    //private readonly ChallengeServiceStorage _challengeServiceStorage;
    private readonly ILogger<BotService> _logger;
    private readonly DiscordSettings _settings;

    public BotService
    (
        DiscordSocketClient client,
        IOptionsMonitor<DiscordSettings> configurationMonitor,
        CommandHandlingService commandHandlingService,
        SlashCommandService slashCommandService,
        //ChallengeServiceStorage challengeServiceStorage,
        ILogger<BotService> logger
    )
    {
        _client = client;
        _commandHandlingService = commandHandlingService;
        _slashCommandService = slashCommandService;
        //_challengeServiceStorage = challengeServiceStorage;
        _logger = logger;
        _settings = configurationMonitor.CurrentValue;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Logging into discord");

        await _client.LoginAsync(TokenType.Bot, _settings.Token);
        await _client.StartAsync();

        while (_client.ConnectionState != ConnectionState.Connected)
        {
            _logger.LogInformation($"Waiting for login State: {_client.ConnectionState}");
            await Task.Delay(300);
        }

        _logger.LogInformation("Initializing services");

        //await SlashCommandAsync();
        await _commandHandlingService.InitializeAsync();
        await _slashCommandService.InitializeAsync();
        //await _challengeServiceStorage.InitializeAsync();

        _logger.LogInformation("The bot has been started");

        //_client.SlashCommandExecuted += async (arg) => await arg.RespondAsync($"You executed {arg.Data.Name}");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Stopped");
    }
}