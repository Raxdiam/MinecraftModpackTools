using CurseForgeNET;
using CurseForgeNET.Models.Files;

namespace MCMPTools.Commands;

internal class SyncCommand : Command
{
    private readonly HttpClient _httpClient;
    private readonly CurseForge _curseClient;
    private readonly Configuration _config;

    public SyncCommand(HttpClient httpClient, CurseForge curseClient, Configuration config) : base("sync")
    {
        _httpClient = httpClient;
        _curseClient = curseClient;
    }

    public override async Task RunAsync(string[] args)
    {
        var dir = Directory.GetCurrentDirectory();
        if (args.Length > 1 && Directory.Exists(args[0])) {
            dir = args[0];
        }

        if (!Util.ValidateInstanceDirectory(dir)) {
            throw new CommandException("Invalid instance directory");
        }

        await SyncMods(dir);
    }

    private async Task SyncMods(string instanceDir)
    {
        var modsDir = Path.Combine(instanceDir, ".minecraft", "mods");
        var indexData = Util.GetIndexData(instanceDir);

        //PurgeUntrackedMods(modsDir, indexData);
        await Manager.GetCommad("purge").RunAsync(new[] { instanceDir });

        Console.CursorVisible = false;
        var i = 1;
        await foreach (var curseFile in GetCurseFiles(indexData)) {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"Downloading {i}/{indexData.Count} mods... ");

            using var res = await _httpClient.GetAsync(curseFile.DownloadUrl);
            await using var stream = await res.Content.ReadAsStreamAsync();
            await using var fs = File.Create(Path.Combine(modsDir, curseFile.FileName));
            await stream.CopyToAsync(fs);
            i++;
        }

        Console.CursorVisible = true;

        Console.WriteLine("Done");
    }

    private async IAsyncEnumerable<CurseFile> GetCurseFiles(IEnumerable<LocalModData> indexData)
    {
        foreach (var localModData in indexData) {
            var curseFile = (await _curseClient.GetModFile(localModData.ProjectId, localModData.FileId)).Data;
            yield return curseFile;
        }
    }
}