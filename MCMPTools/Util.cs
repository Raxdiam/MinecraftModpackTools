using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CurseForgeNET.Models.Files;
using Tommy;

namespace MCMPTools;

internal static partial class Util
{
    private static readonly Regex ModFileRegex = CreateModFileRegex();

    public static List<IndexFile> GetIndexData(string instanceDir)
    {
        var localMods = new List<IndexFile>();

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

            var curseIds = GetCurseIds(updateNode);
            var modrinthIds = GetModrinthIds(updateNode);

            localMods.Add(new() {
                FileName = fileNameNode.AsString,
                Disabled = modFiles.Any(f => f.EndsWith(".disabled")),
                Curse = curseIds == (-1, -1)
                    ? null
                    : new() {
                        FileId = (uint)curseIds.FileId,
                        ProjectId = (uint)curseIds.ProjectId,
                    },
                Modrinth = modrinthIds == (null, null)
                    ? null
                    : new() {
                        ModId = modrinthIds.ModId,
                        Version = modrinthIds.Version,
                    },
            });
        }

        return localMods;
    }

    private static (int FileId, int ProjectId) GetCurseIds(TomlNode updateNode)
    {
        if (!updateNode.TryGetNode("curseforge", out var curseForgeNode))
            return (-1, -1);

        var hasCurseFileId = curseForgeNode.TryGetNode("file-id", out var curseFileIdNode);
        var hasCurseProjectId = curseForgeNode.TryGetNode("project-id", out var curseProjectIdNode);

        return (
            hasCurseFileId ? curseFileIdNode.AsInteger : -1,
            hasCurseProjectId ? curseProjectIdNode.AsInteger : -1
        );
    }

    private static (string ModId, string Version) GetModrinthIds(TomlNode updateNode)
    {
        if (!updateNode.TryGetNode("modrinth", out var modrinthNode))
            return (null, null);

        var hasModrinthModId = modrinthNode.TryGetNode("mod-id", out var modrinthModIdNode);
        var hasModrinthVersion = modrinthNode.TryGetNode("version", out var modrinthVersionNode);

        return (
            hasModrinthModId ? modrinthModIdNode.AsString : null,
            hasModrinthVersion ? modrinthVersionNode.AsString : null
        );
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

    public static string GetSha1(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var bs = new BufferedStream(fs);
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(bs);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }

    public static DownloaderFile ToDownloaderFile(this CurseFile curseFile) =>
        new() {
            Url = curseFile.DownloadUrl,
            FileName = curseFile.FileName,
        };

    public static List<DownloaderFile> ToDownloaderList(this IEnumerable<CurseFile> curseFiles) =>
        curseFiles.Select(ToDownloaderFile).ToList();

    [GeneratedRegex(@"\.jar(\.disabled)?$", RegexOptions.Compiled)]
    private static partial Regex CreateModFileRegex();
}