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

namespace CoderDojo.Management.Discord.Modules
{
    [Group("shortlink", "Shortens a link!")]
    public class LinkShortenerModule : InteractionModuleBase
    {
        //private readonly ChallengeService _challengeService;
        //private readonly ChallengeServiceStorage _serviceStorage;

        //public ChallengeModule
        //(
        //    ChallengeService challengeService,
        //    ChallengeServiceStorage serviceStorage
        //)
        //{
        //    _challengeService = challengeService;
        //    _serviceStorage = serviceStorage;
        //}

        private readonly LinkShortenerService _linkShortenerService;
        private readonly LinkShortenerSettings _settings;

        public LinkShortenerModule(LinkShortenerService linkShortenerService, LinkShortenerSettings settings)
        {
            _linkShortenerService = linkShortenerService;
            _settings = settings;
        }

        [SlashCommand("list", "lists all links")]
        public async Task ListLinks(string searchterm = "")
        {
            //await this.Context.Interaction.RespondAsync("Hello world");

            var items = await _linkShortenerService.GetAllLinksAsync();

            if (!string.IsNullOrWhiteSpace(searchterm))
            {
                var liketerm = searchterm;
                liketerm = !liketerm.StartsWith('*') ? '*' + liketerm : liketerm;
                liketerm = !liketerm.EndsWith('*') ? liketerm + '*' : liketerm;

                items = items.Where(x => x.Id.Contains(searchterm) || LikeOperator.LikeString(x.Id, liketerm, Microsoft.VisualBasic.CompareMethod.Text));
            }

            //await PagedReplyAsync(items.Select(x => new EmbedFieldBuilder
            //{
            //    Name = x.Id,
            //    Value = $"Id: `{x.Id}`\nShortLink: `{x.ShortenedLink}`\nOriginal link: `{x.Url}`"
            //}));

            var builders = items.Select(x => new EmbedFieldBuilder
            {
                Name = x.Id,
                Value = $"Id: `{x.Id}`\nShortLink: `{x.ShortenedLink}`\nOriginal link: `{x.Url}`"
            })
            .Select(x => x.Build())
            .Take(10)
            .ToList();

            var embed = new EmbedBuilder();
            builders.ForEach(x => embed.AddField(x.Name, x.Value));

            //await ReplyAsync(
            //await ReplyAsync()

            await RespondAsync("", new[] { embed.Build() }, ephemeral: true);
            var response = await this.Context.Interaction.GetOriginalResponseAsync();
            
        }
        

        [SlashCommand("hello", "says hello")]
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