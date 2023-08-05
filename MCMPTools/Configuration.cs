using System.Text.Json;

namespace MCMPTools;

internal class Configuration
{
    private const string FlieName = "mcmp-config";

    private static readonly JsonSerializerOptions JsonOptions = new() {
        ReadCommentHandling = JsonCommentHandling.Skip
    };

   public int CursePackManifestVersion { get; set; } = 1;

    /// <summary>
    /// Purge untracked mods when using the sync command.
    /// </summary>
    public bool PurgeOnSync { get; set; } = true;

    /// <summary>
    /// Require confirmation before purging untracked mods.
    /// </summary>
    public bool PurgeOnSyncConfirm { get; set; } = true;

    public string PackOutputDirectory { get; set; } = Directory.GetCurrentDirectory();
    public string PackOutputName { get; set; }
    public string PackVersion { get; set; } = "1.0";
    public string PackAuthor { get; set; } = "Unknown";

    /// <summary>
    /// List of folders to pack into the exported zip.<br/>
    /// These folders are relative to the instance's <c>.minecraft</c> folder.<br/>
    /// ‏‏‎ ‎‏‏‎ ‎(i.e. <c>config</c> is the same as <c>&lt;instance_folder&gt;/.minecraft/config</c>)
    /// </summary>
    public List<string> Overrides { get; set; } = new();

    /// <summary>
    /// List of files to exclude from the exported zip if they exist in any of the <c>Overrides</c> folders.<br/>
    /// These folders are relative to the instance's <c>.minecraft</c> folder.
    /// </summary>
    public List<string> Ignore { get; set; } = new();

    /// <summary>
    /// Load from the first file found within the directory structure with the name "mcmp-config.json" or "mcmp-config.jsonc".
    /// </summary>
    public static Configuration Load()
    {
        var file = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{FlieName}.json*").FirstOrDefault();
        if (file == null)
            return new();

        var json = File.ReadAllText(file);
        if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
            return new();
        
        return JsonSerializer.Deserialize<Configuration>(json, JsonOptions);
    }
}