using CoderDojo.Management.Discord.Services.Interaction;

using CoderDojo.Management.Discord.InteractionServices;

using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DiscordBot.Modules.Services;
using System.Net;
using CoderDojo.Management.Discord.Modules;
using ShlinkDotnet.Extensions;

Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, builder) =>
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        })
        .ConfigureServices((hostContext, services) =>
        {
            services
                .AddOptions()
                .AddHttpClient()
                .AddApplicationInsightsTelemetryWorkerService()

                .Configure<DiscordSettings>(hostContext.Configuration.GetSection("Discord"))
                
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<SlashCommandService>()
                .AddSingleton<CommandService, InjectableCommandService>()
                .AddHostedService<BotService>()
                .AddSingleton<DiscordSocketClient>(x=> new DiscordSocketClient(new DiscordSocketConfig()
                {
                    UseInteractionSnowflakeDate = false
                }))
                .AddSingleton<ButtonEventListener>()
                .AddShlink(hostContext.Configuration.GetSection("shlink"));
            
            AddLinkshortener(services, hostContext.Configuration);

        })
        .Build().Run(); ;


static void AddLinkshortener(IServiceCollection services, IConfiguration configuration)
{
    services.AddTransient<LinkShortenerService>();
    var lsAccessKey = configuration.GetSection("Linkshortener:AccessKey")?.Value;
    services.AddSingleton(new LinkShortenerSettings(lsAccessKey));
    services.AddHttpClient("linkshortener", c =>
    {
        c.BaseAddress = new Uri("https://meet.coderdojo.net/api/");
        c.DefaultRequestHeaders.Add(HttpRequestHeader.ContentType.ToString(), "application/json;charset='utf-8'");
        c.DefaultRequestHeaders.Add(HttpRequestHeader.Accept.ToString(), "application/json");
    });
}