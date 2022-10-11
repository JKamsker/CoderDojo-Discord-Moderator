using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;

using Microsoft.Extensions.Logging;

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

    public SlashCommandService
    (
        DiscordSocketClient discord,
        ILogger<SlashCommandService> logger,
        IServiceProvider serviceProvider
    )
    {
        _interactionService = new InteractionService(discord);
        _client = discord;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        await _interactionService.AddModulesAsync(System.Reflection.Assembly.GetExecutingAssembly(), _serviceProvider);
        await _interactionService.RegisterCommandsToGuildAsync(704990064039559238);


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