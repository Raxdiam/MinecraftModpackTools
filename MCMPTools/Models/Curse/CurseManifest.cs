using System.Text.Json;
using System.Text.Json.Serialization;

namespace MCMPTools.Models.Curse;

public class CurseManifest
{
    [JsonIgnore]
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public CurseManifestMinecraft Minecraft { get; set; }
    public string ManifestType { get; set; }
    public int ManifestVersion { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public List<CurseManifestFile> Files { get; set; }
    public string Overrides { get; set; }

    public static CurseManifest FromJson(string json) => JsonSerializer.Deserialize<CurseManifest>(json, JsonOptions);
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
}