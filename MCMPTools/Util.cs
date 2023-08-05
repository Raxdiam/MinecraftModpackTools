using System.Text.RegularExpressions;
using Tommy;

namespace MCMPTools;

internal partial class Util
{
    private static readonly Regex ModFileRegex = CreateModFileRegex();

    public static List<LocalModData> GetIndexData(string instanceDir)
    {
        var localMods = new List<LocalModData>();

        var modsDir = Path.Combine(instanceDir, ".minecraft", "mods");
        var indexDir = Path.Combine(modsDir, ".index");
        var modFiles = Directory.GetFiles(modsDir).Where(f => ModFileRegex.IsMatch(f)).ToArray();
        var tomlFiles = Directory.GetFiles(indexDir, "*.toml");

        foreach (var file in tomlFiles) {
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
                Disabled = modFiles.Any(f => f.EndsWith(".disabled")),
                FileId = (uint)fileIdNode.AsInteger,
                ProjectId = (uint)projectIdNode.AsInteger
            });
        }

        return localMods;
    }

    public static bool ValidateInstanceDirectory(string path)
    {
        if (!Directory.Exists(path)) return false;
        if (!Directory.Exists(Path.Combine(path, ".minecraft", "mods", ".index"))) return false;
        if (!File.Exists(Path.Combine(path, "mmc-pack.json"))) return false;

        return true;
    }

    public static string NormalizeFileName(string fileName, string replaceWhiteSpaceWith, string replaceInvalidCharsWith)
    {
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        fileName = fileName.Replace(' ', replaceWhiteSpaceWith[0]);
        var normalizedFileName = string.Join(replaceInvalidCharsWith, fileName.Split(invalidFileNameChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        return normalizedFileName;
    }

    [GeneratedRegex(@"\.jar(\.disabled)?$", RegexOptions.Compiled)]
    private static partial Regex CreateModFileRegex();
}