using Discord;
using Discord.WebSocket;

//using DiscordBot.Domain.Configuration;
//using DiscordBot.Modules.Utils.ReactionBase;


namespace CoderDojo.Management.Discord.InteractionServices;

public class ButtonEventListener : IDisposable
{
    private readonly DiscordSocketClient _socketClient;

    public ButtonEventListener(DiscordSocketClient socketClient)
    {
        _socketClient = socketClient;
        _socketClient.ButtonExecuted += ButtonExecuted;
    }

    private Dictionary<ulong, Func<SocketMessageComponent, Task>> _subscriptions = new();

    public void Subscribe(ulong messageId, Func<SocketMessageComponent, Task> @event)
    {
        _subscriptions[messageId] = @event;
    }

    private async Task ButtonExecuted(SocketMessageComponent arg)
    {
        if (!_subscriptions.TryGetValue(arg.Message.Id, out var handler))
        {
            await arg.RespondAsync
            (
                "Sorry, I've lost my memory for this action :( \r" +
                "https://giphy.com/gifs/FourRestFilms-social-distancing-four-rest-films-fourrestfilms-XEmTZ0RX0KJTS3gn4w"
            );
            return;
        }
        await handler(arg);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _socketClient.ButtonExecuted -= ButtonExecuted;
        }
    }


    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
