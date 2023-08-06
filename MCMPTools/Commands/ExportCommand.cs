using System.IO.Compression;
using MCMPTools.Models.Curse;
using MCMPTools.Models.MMC;

namespace MCMPTools.Commands;

internal class ExportCommand : Command
{
    private static readonly Dictionary<string, string> ValidLoaders = new() {
        { "net.minecraftforge", "forge" },
        { "net.fabricmc.fabric-loader", "fabric" }
    };

    private readonly Configuration _config;

    public ExportCommand(Configuration config) : base("export")
    {
        _config = config;
    }
    
    public override Task RunAsync(string[] args)
    {
        var instDir = Directory.GetCurrentDirectory();
        if (args.Length > 0 && Directory.Exists(args[0])) {
            instDir = args[0];
        }

        if (!Util.ValidateInstanceDirectory(instDir)) {
            throw new CommandException("Invalid instance directory");
        }

        var manifest = CreateCurseManifest(instDir);
        var manifestJson = manifest.ToJson();
        
        var tempDir = Path.Combine(Path.GetTempPath(), ".mcmpt-export");
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);

        Directory.CreateDirectory(tempDir);

        var manifestFile = Path.Combine(tempDir, "manifest.json");
        File.WriteAllText(manifestFile, manifestJson);

        var overridesDir = Path.Combine(tempDir, "overrides");
        Directory.CreateDirectory(overridesDir);

        var overrides = _config.Overrides;
        var ignored = _config.Ignore;

        foreach (var o in overrides) {
            var source = Path.Combine(instDir, ".minecraft", o);
            if (!Directory.Exists(source)) {
                //throw new CommandException($"Could not find override folder: {o}");
                Console.WriteLine($"Skipping override folder as it could not be found: {o}");
                continue;
            }

            var destination = Path.Combine(overridesDir, o);
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories)) {
                var relativePath = Path.GetRelativePath(source, file);
                if (ignored.Contains(relativePath))
                    continue;

                var dest = Path.Combine(destination, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest);
            }
        }

        // Create the zip file, contents of the temp directory should be at the root of the zip
        var packName = _config.PackOutputName ?? Util.NormalizeFileName(manifest.Name, "_", "");
        var exportDir = Path.GetFullPath(_config.PackOutputDirectory);

        if (!Directory.Exists(exportDir))
            Directory.CreateDirectory(exportDir);

        var zipFile = Path.Combine(exportDir, $"{packName}-{manifest.Version}.zip");
        if (File.Exists(zipFile))
            File.Delete(zipFile);

        ZipFile.CreateFromDirectory(tempDir, zipFile);

        Console.WriteLine($"Successfully exported modpack to '{zipFile}'.");

        return Task.CompletedTask;
    }

    private CurseManifest CreateCurseManifest(string instDir)
    {
        var mmcManifestFile = Path.Combine(instDir, "mmc-pack.json");
        if (!File.Exists(mmcManifestFile))
            throw new CommandException("Could not find MMC Pack manifest file.");

        var mmcManifestJson = File.ReadAllText(mmcManifestFile);
        var mmcManifest = MmcManifest.FromJson(mmcManifestJson);

        var mmcMinecraftComponent = mmcManifest.Components.FirstOrDefault(c => c.CachedName == "Minecraft") ??
                                    throw new CommandException("Could not find Minecraft component in MMC Pack manifest.");
        var mmcLoaderComponent = mmcManifest.Components.FirstOrDefault(c => ValidLoaders.ContainsKey(c.Uid)) ??
                                 throw new CommandException("Could not find a valid mod loader component in MMC Pack manifest.");

        return new() {
            Minecraft = new() {
                Version = mmcMinecraftComponent.Version,
                ModLoaders = new() {
                    new() {
                        Id = $"{ValidLoaders[mmcLoaderComponent.Uid]}-{mmcLoaderComponent.Version}",
                        Primary = true
                    }
                }
            },
            ManifestType = "minecraftModpack",
            ManifestVersion = _config.CursePackManifestVersion,
            Name = GetInstanceName(instDir),
            Version = _config.PackVersion,
            Author = _config.PackAuthor,
            Files = Util.GetIndexData(instDir).Select(d => new CurseManifestFile {
                ProjectId = (int)d.Curse.ProjectId,
                FileId = (int)d.Curse.FileId,
                Required = !d.Disabled
            }).ToList(),
            Overrides = "overrides"
        };
    }

    private static string GetInstanceName(string instanceDir)
    {
        var file = Path.Combine(instanceDir, "instance.cfg");
        if (!File.Exists(file))
            throw new CommandException("Could not find instance.cfg");

        var kv = File.ReadAllLines(file)
            .Where(s => s.Contains('='))
            .Select(s => s.Split('='))
            .ToDictionary(x => x[0], x => x[1]);

        if (!kv.ContainsKey("name"))
            throw new CommandException("Could not find instance name in instance.cfg");

        return kv["name"];
    }

    /*private static string GetValidFileName()
    {
        var invalidFileName = new string(Path.GetInvalidFileNameChars());
        string fileName;

        while (true) {
            Console.Write("Enter a name for the exported modpack (must be a valid file name): ");
            fileName = Console.ReadLine();

            if (!fileName!.Any(invalidFileName.Contains)) {
                break;
            }

            Console.WriteLine("Invalid file name. Please try again.");
        }

        return fileName;
    }*/
}