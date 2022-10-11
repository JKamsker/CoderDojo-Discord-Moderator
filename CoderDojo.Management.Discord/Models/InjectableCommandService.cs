using Discord.Commands;

//using DiscordBot.Domain.Configuration;
//using DiscordBot.Modules.Utils.ReactionBase;

using Microsoft.Extensions.Options;

namespace CoderDojo.Management.Discord.InteractionServices;

public class InjectableCommandService : CommandService
{
    public InjectableCommandService(IOptions<CommandServiceConfig> config) : base(config.Value)
    {
    }
}