using Discord;
using Discord.WebSocket;
using Duthie.Bot.Utils;

namespace Duthie.Bot.Extensions;

public static class SocketSlashCommandExtensions
{
    public static async Task SendErrorAsync(this SocketSlashCommand command, string? text = null, Embed[]? embeds = null, bool isTTS = false, bool ephemeral = true, AllowedMentions? allowedMentions = null, MessageComponent? components = null, Embed? embed = null, RequestOptions? options = null)
    {
        var texts = MessageUtils.Chunk(text);

        if (texts?.Count() > 0)
            await command.SendErrorAsync(texts, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
        else
            await command.RespondAsync(null, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);

    }

    public static async Task SendErrorAsync(this SocketSlashCommand command, IEnumerable<string> texts, Embed[]? embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions? allowedMentions = null, MessageComponent? components = null, Embed? embed = null, RequestOptions? options = null) =>
        await command.RespondAsync(texts, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);

    public static async Task RespondAsync(this SocketSlashCommand command, IEnumerable<string> texts, Embed[]? embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions? allowedMentions = null, MessageComponent? components = null, Embed? embed = null, RequestOptions? options = null)
    {
        foreach (var text in texts)
        {
            if (!command.HasResponded)
                await command.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
            else if (ephemeral)
                await command.FollowupAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
            else
                await command.Channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, null, components, null, embeds, MessageFlags.None);
        }
    }
}