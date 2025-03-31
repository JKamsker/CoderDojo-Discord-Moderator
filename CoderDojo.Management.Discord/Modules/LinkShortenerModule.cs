//using CoderDojo.Management.Discord.Services.Challenge;

using Discord;

//using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using DiscordBot.Modules.Services;

using System.Linq;
using System.Text;

//using global::System.Linq.Async;

//using linqasync::System.Linq;
using Microsoft.VisualBasic.CompilerServices;
using CoderDojo.Management.Discord.InteractionServices;
using MongoDB.Driver;

namespace CoderDojo.Management.Discord.Modules
{
    [Group("shortlink", "Shortens a link!")]
    public class LinkShortenerModule : InteractionModuleBase
    {
        private readonly LinkShortenerService _linkShortenerService;
        private readonly LinkShortenerSettings _settings;
        private readonly ButtonEventListener _buttonEventListener;

        public LinkShortenerModule
        (
            LinkShortenerService linkShortenerService,
            LinkShortenerSettings settings,
            ButtonEventListener buttonEventListener
        )
        {
            _linkShortenerService = linkShortenerService;
            _settings = settings;
            _buttonEventListener = buttonEventListener;
        }

        [SlashCommand("list", "lists all links")]
        public async Task ListLinks(string searchterm = "")
        {
            if (!await CheckPermissions())
            {
                return;
            }

            var builders = await GetSearchResults(searchterm);

            var page = 0;
            var pageSize = 5;
            var maxPage = (int)Math.Ceiling((decimal)(builders.Count / pageSize));


            await RespondAsync
            (
                $"Page {page}/{builders.Count / pageSize} | {builders.Count} Items",
                embeds: BuildPagedEmbed(builders, page, pageSize).ToArray(),
                ephemeral: true,
                components: BuildPagingButtons(page, maxPage)
            );

            var response = await this.Context.Interaction.GetOriginalResponseAsync();
            Console.WriteLine("ResponseId:" + response.Id);

            _buttonEventListener.Subscribe(response.Id, async x =>
            {
                page = x.Data.CustomId switch
                {
                    "+" => page + 1,
                    "-" => page + -1,

                    "--" => 0,
                    "++" => maxPage,
                    _ => 0
                };

                page = Math.Max(0, page);
                page = Math.Min(maxPage, page);

                await x.UpdateAsync(m =>
                {
                    m.Content = new($"Page {page}/{builders.Count / pageSize} | {builders.Count} Items");
                    m.Embeds = new(BuildPagedEmbed(builders, page, pageSize).ToArray());
                    m.Components = BuildPagingButtons(page, maxPage);
                });

            });

            static MessageComponent BuildPagingButtons(int page, int maxPage)
            {
                var canNavigateLeft = page > 0;
                var canNavigateRight = page < maxPage;

                var builder = new ComponentBuilder()
                    .WithButton("|<<", "--", disabled: !canNavigateLeft)
                    .WithButton("<<", "-", disabled: !canNavigateLeft)
                    .WithButton(">>", "+", disabled: !canNavigateRight)
                    .WithButton(">>|", "++", disabled: !canNavigateRight)
                ;

                var components = builder.Build();
                return components;
            }

            static IEnumerable<Embed> BuildPagedEmbed(List<EmbedField> builders, int page, int pageSize)
            {
                return BuildEmbed(builders.Skip(page * pageSize).Take(pageSize));
            }

            static IEnumerable<Embed> BuildEmbed(IEnumerable<EmbedField> builders)
            {
                foreach (var x in builders)
                {
                    var embedBuilder = new EmbedBuilder();

                    embedBuilder.AddField(x.Name, x.Value);
                    yield return embedBuilder.Build();
                }
            }

            async Task<List<EmbedField>> GetSearchResults(string searchterm)
            {
                var items = await _linkShortenerService.GetAllLinksAsync();
                if (!string.IsNullOrWhiteSpace(searchterm))
                {
                    var liketerm = searchterm;
                    liketerm = !liketerm.StartsWith('*') ? '*' + liketerm : liketerm;
                    liketerm = !liketerm.EndsWith('*') ? liketerm + '*' : liketerm;

                    items = items.Where(x => x.Id.Contains(searchterm) || LikeOperator.LikeString(x.Id, liketerm, Microsoft.VisualBasic.CompareMethod.Text));
                }

                var builders = items.Select(x => new EmbedFieldBuilder
                {
                    Name = x.Id,
                    Value = $"Id: `{x.Id}`\nShortLink: `{x.ShortenedLink}`\nOriginal link: `{x.Url}`"
                })
                .Select(x => x.Build())
                .ToList();
                return builders;
            }
        }




        [SlashCommand("create", "creates shortlink")]
        public async Task CreateLinkAsync(string id, string link)
        {
            if (!await CheckPermissions())
            {
                return;
            }

            await RespondAsync($"Creating {id} to {link}!", ephemeral: true);
            await _linkShortenerService.ShortenUrl(id, _settings.AccessKey, link);
            await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = $"Create complete! Ready at https://meet.coderdojo.net/{id}");
        }


        [SlashCommand("update", "updates shortlink")]
        public async Task UpdateLinkAsync(string id, string link)
        {
            if (!await CheckPermissions())
            {
                return;
            }

            await RespondAsync($"Updating {id} to {link}!", ephemeral: true);
            await _linkShortenerService.UpdateUrlAsync(id, _settings.AccessKey, link);
            await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = $"Update complete! Ready at http://meet.coderdojo.net/{id}");
        }


        private async Task<bool> CheckPermissions()
        {
            var user = base.Context.User;
            if (user is not IGuildUser gu)
            {
                await RespondAsync("https://dontgetserious.com/wp-content/uploads/2022/01/Wait-A-Minute-Memes-768x577.jpeg", ephemeral: true);
                return false;
            }

            var isAllowed = gu.GuildPermissions.Administrator
                || gu.RoleIds?.Contains((ulong)704990383293333534) == true;
            
            if (!isAllowed)
            {
                await RespondAsync("https://img-9gag-fun.9cache.com/photo/a1rL3vY_700bwp.webp", ephemeral: true);
                return false;
            }
            return true;
        }
    }
}