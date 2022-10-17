using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using System.Diagnostics;

//using DiscordBot.Domain.Configuration;
//using DiscordBot.Modules.Utils.ReactionBase;
namespace CoderDojo.Management.Discord.Services.Interaction;

public class SlashCommandService
{
    private readonly InteractionService _interactionService;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<SlashCommandService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSettings _settings;

    public SlashCommandService
    (
        DiscordSocketClient discord,
        IOptions<DiscordSettings> options,
        ILogger<SlashCommandService> logger,
        IServiceProvider serviceProvider
    )
    {
        _interactionService = new InteractionService(discord);
        _client = discord;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _settings = options.Value;
    }

    public async Task InitializeAsync()
    {
        await _interactionService.AddModulesAsync(System.Reflection.Assembly.GetExecutingAssembly(), _serviceProvider);
        if (_settings.MainServerId is not null)
        {
            await _interactionService.RegisterCommandsToGuildAsync(_settings.MainServerId.Value);
        }


        _interactionService.AutocompleteCommandExecuted += async (a, b, c) =>
        {
            //Debugger.Break();
        };

        _interactionService.AutocompleteHandlerExecuted += async (autocompleteHandler, interactionContext, result) =>
        {
            //Debugger.Break();
        };

        _client.ButtonExecuted += async (interaction) =>
        {
            var ctx = new SocketInteractionContext<SocketMessageComponent>(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        };

        _client.SlashCommandExecuted += async (interaction) =>
        {
            var ctx = new SocketInteractionContext<SocketSlashCommand>(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        };

        _client.AutocompleteExecuted += async (interaction) =>
        {
            var ctx = new SocketInteractionContext<SocketAutocompleteInteraction>(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        };
    }
}