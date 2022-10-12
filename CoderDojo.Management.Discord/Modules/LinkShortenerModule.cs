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
        }



        [SlashCommand("add", "adds a shortlink")]
        public async Task HelloWorld() //string searchString = ""
        {
            var builder = new ComponentBuilder()
                .WithButton("label", "custom-id");

            //await ReplyAsync("Here is a button!", components: builder.Build());

            await this.Context.Interaction.RespondAsync("Hello world", components: builder.Build());

            //var embeds = _challengeService
            //    .ListAsync(base.Context.User.Id)
            //    //.SelectMany(x => x.Challenges.ToAsyncEnumerable())
            //    .Select(x => new EmbedFieldBuilder
            //    {
            //        Name = x.ChallengeData.Name,
            //        Value = $"Id: {x.RenderIdentifier()}\n" +
            //                $"Name: *{x.ChallengeData.Name}*\n" +
            //                $"Description: *{x.ChallengeData.Description}*\n" +
            //                $"Prerequisits: {x.RenderPreRequisits()}"
            //    })
            //    ;

            //var eb = new EmbedBuilder();
            //await foreach (var embed in embeds)
            //{
            //    eb.AddField(embed);
            //}

            //await this.Context.Interaction.RespondAsync(embed: eb.Build());
            //var response = await this.Context.Interaction.GetOriginalResponseAsync();
        }

        //[SlashCommand("start", "Start a new challenge!")]
        //public async Task StartAsync(string identifier)
        //{
        //    var challenge = await _challengeService.StartChallengeAsync(identifier, Context.User.Id);
        //    if (challenge is null)
        //    {
        //        await RespondAsync("`Sorry, i can't find that challenge`");
        //        return;
        //    }

        //    var challengeText = string.IsNullOrWhiteSpace(challenge.Message)
        //        ? "The challenge has no Text, sorry :("
        //        : challenge.Message;

        //    await RespondAsync(challengeText, ephemeral: true);

        //    if (challenge.Attachments?.Length is not null and > 0)
        //    {
        //        var streams = challenge.Attachments
        //            .Select(x => Encoding.UTF8.GetBytes(x))
        //            .Select(x => new MemoryStream(x))
        //            .Select((x, i) => new FileAttachment(x, i > 1 ? $"ChallengeAttachment-{i}.txt" : "ChallengeAttachment.txt"))
        //            .ToList();

        //        // await Context.Channel.SendFilesAsync(streams);

        //        await Context.Interaction.FollowupWithFilesAsync(streams, "Your input", ephemeral: true);

        //    }

        //    //await Context.Channel.SendFileAsync(Stream.Null, "Info.Text");
        //}

        //[SlashCommand("solve", "Send your solution!")]
        //public async Task SolveAsync(string identifier, string solution)
        //{
        //    var challengeResult = await _challengeService.SolveChallengeAsync(Context.User.Id, new ChallengeSolutionRequestDto
        //    {
        //        ChallengeIdentifier = identifier,
        //        Result = solution
        //    });

        //    var message = challengeResult.Success ? "Success!" : "No success.";
        //    if (!string.IsNullOrEmpty(challengeResult.Message))
        //    {
        //        message += $"\n{challengeResult.Message}";
        //    }

        //    await this.Context.Interaction.RespondAsync(message);
        //}

        //[AutocompleteCommand("identifier", "challenge start")]
        //public async Task Autocomplete()
        //{
        //    ListAsync<AutocompleteResult> results = new ListAsync<AutocompleteResult>();

        //    results.Add(new AutocompleteResult("identifier", "ayay"));
        //    results.Add(new AutocompleteResult("identifier", "xA"));

        //    if (Context.Interaction is SocketAutocompleteInteraction sai)
        //    {
        //        await sai.RespondAsync(results);
        //    }
        //}
    }
}