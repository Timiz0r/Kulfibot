namespace Kulfibot.Discord
{
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    public record DiscordSecrets(
        string BotToken)
    {
        public static async Task<DiscordSecrets> FromFileAsync(string secretsPath)
        {
            DiscordSecrets secrets =
                JsonSerializer.Deserialize<DiscordSecrets>(await File.ReadAllTextAsync(secretsPath))!;
            return secrets;
        }
    }
}
