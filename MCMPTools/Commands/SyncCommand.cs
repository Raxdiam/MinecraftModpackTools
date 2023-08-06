using CurseForgeNET;
using CurseForgeNET.Models.Files;
using MCMPTools.Rendering;
using Spectre.Console;

namespace MCMPTools.Commands;

internal class SyncCommand : Command
{
    private readonly CurseForge _curseClient;

    public SyncCommand(CurseForge curseClient) : base("sync")
    {
        _curseClient = curseClient;
    }

    /// <summary>
    /// Synchronize mods folder with the index (tracking) files
    /// </summary>
    /// <param name="args">args[0]: Instance directory.<br/>args[1]: Force sync (don't skip existing files).</param>
    public override async Task RunAsync(string[] args)
    {
        var dir = Directory.GetCurrentDirectory();
        if (args.Length > 1 && Directory.Exists(args[0])) {
            dir = args[0];
        }

        if (!Util.ValidateInstanceDirectory(dir)) {
            throw new CommandException("Invalid instance directory");
        }

        var force = args.Length > 0 && (args.Contains("--force") || args.Contains("-f"));

        await SyncMods(dir, force);
    }

    private async Task SyncMods(string instanceDir, bool force)
    {
        var modsDir = Path.Combine(instanceDir, ".minecraft", "mods");

        await Manager.GetCommad("purge").RunAsync(new[] { instanceDir });

        await AnsiConsole.Progress()
            .HideCompleted(true)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new CountColumn()
            )
            .StartAsync(async ctx => {
                var indexData = Util.GetIndexData(instanceDir);
                AnsiConsole.MarkupLine($"Tracked mods found: {indexData.Count}");
                var curseFiles = await GetAllCurseFiles(indexData, ctx);
                if (!force) {
                    curseFiles = curseFiles.Where(f => !File.Exists(Path.Combine(modsDir, f.FileName))).ToList();
                }
                else {
                    AnsiConsole.MarkupLine("[yellow]Forcing synchronization. Existing files will be overwritten.[/]");
                }

                if (curseFiles.Count == 0) {
                    AnsiConsole.MarkupLine("[green]All mods present or none are tracked. No synchronization needed.[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"Mods requiring synchronization: {curseFiles.Count}");

                var task = ctx.AddTask("Downloading mods", maxValue: curseFiles.Count);
                using var download = new Downloader(modsDir);
                await download.DownloadFiles(curseFiles.ToDownloaderList(), _ => {
                    task.Increment(1);
                });
                
                AnsiConsole.MarkupLine("[green]Synchronization completed.[/]");
            });
    }

    private async Task<List<CurseFile>> GetAllCurseFiles(List<IndexFile> indexData, ProgressContext ctx)
    {
        var task = ctx.AddTask("Gathering mod data", maxValue: indexData.Count);
        var curseFiles = new List<CurseFile>();
        foreach (var localModData in indexData) {
            if (localModData.Curse != null && (localModData.Curse.ProjectId > 1 || localModData.Curse.FileId > 1)) {
                var curseFile = (await _curseClient.GetModFile(localModData.Curse.ProjectId, localModData.Curse.FileId)).Data;
                curseFiles.Add(curseFile);
            }

            if (localModData.Modrinth != null) {
                //TODO: Add modrinth support
            }
            
            task.Increment(1);
        }

        return curseFiles;
    }
    
}