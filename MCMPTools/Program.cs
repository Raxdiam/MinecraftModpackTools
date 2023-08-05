using CurseForgeNET;
using CurseForgeNET.Models.Files;
using MCMPTools.Commands;
using Tommy;

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
            .RegisterCommand(new SyncCommand(Http, CurseClient, Config))
            .RegisterCommand(new ExportCommand(Config));
        await commandManager.RunAsync(args);


        /*if (args[0] == "sync") {
            var dir = Directory.GetCurrentDirectory();
            if (args.Length > 1 && Directory.Exists(args[1])) {
                dir = args[1];
            }

            var isValidInstance = ValidateInstanceDirectory(dir);
            if (!isValidInstance) {
                Console.WriteLine("Invalid instance directory");
                return;
            }

            await SyncMods(dir);
        }*/
    }

    private static List<LocalModData> GetIndexData(string instanceDir)
    {
        var localMods = new List<LocalModData>();

        var indexDir = Path.Combine(instanceDir, ".minecraft", "mods", ".index");
        var files = Directory.GetFiles(indexDir, "*.toml");

        foreach (var file in files) {
            var tomlName = Path.GetFileName(file);
            using var reader = File.OpenText(file);
            var toml = TOML.Parse(reader);

            if (!toml.TryGetNode("filename", out var fileNameNode)) {
                Console.WriteLine($"File name is missing in '{tomlName}'");
                return null;
            }

            if (!toml.TryGetNode("update", out var updateNode)) {
                Console.WriteLine($"Update table is missing in '{tomlName}'");
                return null;
            }

            if (!updateNode.TryGetNode("curseforge", out var curseForgeNode)) {
                Console.WriteLine($"CurseForge table is missing in '{tomlName}'");
                return null;
            }

            if (!curseForgeNode.TryGetNode("file-id", out var fileIdNode)) {
                Console.WriteLine($"File ID is missing in '{tomlName}'");
                return null;
            }

            if (!curseForgeNode.TryGetNode("project-id", out var projectIdNode)) {
                Console.WriteLine($"Project ID is missing in '{tomlName}'");
                return null;
            }

            localMods.Add(new() {
                FileName = fileNameNode.AsString,
                FileId = (uint)fileIdNode.AsInteger,
                ProjectId = (uint)projectIdNode.AsInteger
            });
        }

        return localMods;
    }

    private static async IAsyncEnumerable<CurseFile> GetCurseFiles(IEnumerable<LocalModData> indexData)
    {
        foreach (var localModData in indexData) {
            var curseFile = (await CurseClient.GetModFile(localModData.ProjectId, localModData.FileId)).Data;
            yield return curseFile;
        }
    }

    private static void PurgeUntrackedMods(string modsDir, IReadOnlyCollection<LocalModData> indexData)
    {
        var purgedCount = 0;
        var files = Directory.GetFiles(modsDir);
        foreach (var file in files) {
            var fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".disabled")) 
                fileName = fileName[..^9];
            
            var isTracked = indexData.Any(x => x.FileName == fileName);
            if (isTracked) continue;
            File.Delete(file);
            purgedCount++;
        }

        if (purgedCount > 0)
            Console.WriteLine($"Purged {purgedCount} untracked mods");
    }

    private static async Task SyncMods(string instanceDir)
    {
        var modsDir = Path.Combine(instanceDir, ".minecraft", "mods");
        var indexData = GetIndexData(instanceDir);

        PurgeUntrackedMods(modsDir, indexData);

        Console.CursorVisible = false;
        var i = 1;
        await foreach (var curseFile in GetCurseFiles(indexData)) {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"Downloading {i}/{indexData.Count} mods... ");

            using var res = await Http.GetAsync(curseFile.DownloadUrl);
            await using var stream = await res.Content.ReadAsStreamAsync();
            await using var fs = File.Create(Path.Combine(modsDir, curseFile.FileName));
            await stream.CopyToAsync(fs);
            i++;
        }
        Console.CursorVisible = true;

        Console.WriteLine("Done");
    }

    /*private static string FindIndexFolder(string path)
    {
        if (Path.GetFileName(path) == ".index") return path;
        var dirs = Directory.GetDirectories(path, ".index", SearchOption.AllDirectories);
        if (dirs.Length > 0) return dirs[0];
        
        throw new Exception("Could not find .index folder");
    }*/

    private static bool ValidateInstanceDirectory(string path)
    {
        if (!Directory.Exists(path)) return false;
        if (!Directory.Exists(Path.Combine(path, ".minecraft", "mods", ".index"))) return false;
        if (!File.Exists(Path.Combine(path, "mmc-pack.json"))) return false;

        return true;
    }
}

/*public class LocalModData
{
    [DataMember(Name = "name")]
    public string Name { get; set; }
    [DataMember(Name = "filename")]
    public string FileName { get; set; }
    [DataMember(Name = "side")]
    public string Side { get; set; }
    
}

public class ModDownload
{
    [DataMember(Name = "mode")]
    public string Mode { get; set; }
    [DataMember(Name = "url")]
    public string Url { get; set; }
    [DataMember(Name = "hash-format")]
    public string HashFormat { get; set; }
    [DataMember(Name = "hash")]
    public string Hash { get; set; }
}

public class Update
{
    [DataMember(Name = "curseforge")]
    public UpdateCurseforge CurseForge { get; set; }
}

public class UpdateCurseforge
{
    [DataMember(Name = "file-id")]
    public int FileId { get; set; }
    [DataMember(Name = "project-id")]
    public int ProjectId { get; set; }
}*/