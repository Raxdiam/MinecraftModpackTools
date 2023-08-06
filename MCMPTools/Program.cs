using CurseForgeNET;
using MCMPTools.Commands;

namespace MCMPTools;

internal class Program
{
    public static CurseForge CurseClient;
    public static readonly HttpClient Http = new();
    public static readonly Configuration Config = Configuration.Load();

    private static async Task Main(string[] args)
    {
        CurseClient = new(
            "$2a$10$bL4bIL5pUWqfcO7KQtnMReakwtfHbNKh6v1uTpKlzhwoueEJQnPnm",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 OverwolfClient/0.204.0.1");

        var commandManager = new CommandManager()
            .RegisterCommand(new PurgeCommand())
            .RegisterCommand(new SyncCommand(CurseClient))
            .RegisterCommand(new ExportCommand(Config));
        await commandManager.RunAsync(args);
    }
}
