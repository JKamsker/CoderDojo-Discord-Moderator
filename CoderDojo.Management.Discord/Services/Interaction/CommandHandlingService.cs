using Discord;
using Discord.Commands;
using Discord.WebSocket;

//using DiscordBot.Domain.Configuration;
//using DiscordBot.Modules.Utils.ReactionBase;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CoderDojo.Management.Discord.InteractionServices;
public partial class CommandHandlingService
{
    private readonly CommandService _commands;
    private readonly DiscordSocketClient _discord;
    private readonly IOptionsMonitor<DiscordSettings> _discordSettings;
    private readonly ILogger<CommandHandlingService> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly IServiceProvider _services;
    //private readonly CommandSuggestionsService _commandSuggestions;

    private string? _messagePrefix = null;

    private readonly ConcurrentDictionary<ulong, IServiceScope> _scopes = new ConcurrentDictionary<ulong, IServiceScope>();

    public CommandHandlingService
    (
        IServiceProvider services,
        CommandService commandService,
        //CommandSuggestionsService commandSuggestions,
        DiscordSocketClient discordSocketClient,
        IOptionsMonitor<DiscordSettings> configurationMonitor,
        ILogger<CommandHandlingService> logger,
        TelemetryClient telemetryClient
    )
    {
        _services = services;
        _commands = commandService;
        //_commandSuggestions = commandSuggestions;
        _discord = discordSocketClient;

        _discordSettings = configurationMonitor;
        _logger = logger;
        _telemetryClient = telemetryClient;
        InitializePrefix();

        // Hook CommandExecuted to handle post-command-execution logic.
        _commands.CommandExecuted += CommandExecutedAsync;
        // Hook MessageReceived so we can process each message to see
        // if it qualifies as a command.
        _discord.MessageReceived += MessageReceivedAsync;

        //_discord.ReactionAdded += (x, y, z) => ReactionAction(x, y, z, ReactionType.Added);
        //_discord.ReactionRemoved += (x, y, z) => ReactionAction(x, y, z, ReactionType.Removed);
        //_discord.ReactionsCleared += (x, y) => ReactionAction(x, y, null, ReactionType.Cleared);

        //_discord.ButtonExecuted += ButtonExecuted;
    }

    //private async Task ButtonExecuted(SocketMessageComponent arg)
    //{
    //    // ResponseId == arg.Message.Id
    //    // arg.Message.Id == 1029825860863283220
    //    // arg.Data.CustomId == custom-id

    //    Console.WriteLine("Button executed!");
    //    await arg.RespondAsync("Arrrr");
    //}

    private void InitializePrefix()
    {
        _messagePrefix = _discordSettings.CurrentValue.CommandPrefix;
        _discordSettings.OnChange(x =>
        {
            _messagePrefix = x.CommandPrefix;
        });
    }

    public async Task InitializeAsync()
    {
        // Register modules that are public and inherit ModuleBase<T>.
        foreach (var item in AppDomain.CurrentDomain.GetAssemblies()) //.Append(Assembly.Load("DiscordBot.Modules")).Distinct()
        {
            var modules = (await _commands.AddModulesAsync(item, _services)).ToList();
            if (modules.Count == 0)
            {
                continue;
            }

            _logger.Log(LogLevel.Information, $"{modules.Count} Modules were added. ({string.Join('|', modules.Select(m => m.Name))})");
        }
    }

    public async Task MessageReceivedAsync(SocketMessage rawMessage)
    {
        // Ignore system messages, or messages from other bots
        if (!(rawMessage is SocketUserMessage message)) return;
        if (message.Source != MessageSource.User) return;

        // This value holds the offset where the prefix ends
        var argPos = 0;
        // Perform prefix check. You may want to replace this with
        // (!message.HasCharPrefix('!', ref argPos))
        // for a more traditional command format like !help.
        //if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

        if (_messagePrefix == null)
        {
            _logger.Log(LogLevel.Information, $"Kein prefix gefunden.");
            return;
        }

        if (!message.HasStringPrefix(_messagePrefix, ref argPos) && !(message.Channel is IPrivateChannel))
        {
            return;
        }

        var context = new SocketCommandContext(_discord, message);

        //The discordNET client doesnt create a scope for us, so we have to care about it
        var scope = _services.CreateScope();
        _scopes[message.Id] = scope;

        using var operation = _telemetryClient.StartOperation<RequestTelemetry>(context.Message.Content);
        operation.Telemetry.Properties.Add("User", context.User.Username);

        // Perform the execution of the command. In this method,
        // the command service will perform precondition and parsing check
        // then execute the command if one is matched.
        var result = await _commands.ExecuteAsync(context, argPos, scope.ServiceProvider);

        // Note that normally a result will be returned by this format, but here
        // we will handle the result in CommandExecutedAsync,
        operation.Telemetry.Success = result.IsSuccess;
        _telemetryClient.StopOperation(operation);
    }

    public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        if (_scopes.TryRemove(context.Message.Id, out var scopeToDispose))
        {
            scopeToDispose.Dispose();
        }

        // command is unspecified when there was a search failure (command not found); we don't care about these errors
        if (!command.IsSpecified)
        {
            var message = $"Command not found: {context?.Message?.Content ?? "[NOT_FOUND]" }";
            _logger.LogInformation(message);
            _logger.LogInformation(message);
            // await (context?.Message.ReplyAsync(context.Message.Content) ?? Task.CompletedTask);
            //await _commandSuggestions.SuggestCommand(context, _discordSettings.CurrentValue.CommandPrefix[0]);
            return;
        }

        // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
        if (result.IsSuccess)
            return;

        // the command failed, let's notify the user that something happened.
        if (result is ExecuteResult executeResult && executeResult.Exception != null)
        {
            _logger.LogError(executeResult.Exception, executeResult.Exception.Message);
            await context.Channel.SendMessageAsync($"Failed executing the command: ```\n{executeResult.Exception}```");
        }
        else
        {
            await context.Channel.SendMessageAsync($"Failed executing the command: ```\n{result}```");
        }
    }
}
