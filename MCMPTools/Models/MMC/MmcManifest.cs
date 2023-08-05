using System.Text.Json;
using System.Text.Json.Serialization;

namespace MCMPTools.Models.MMC;

public class MmcManifest
{
    [JsonIgnore]
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<MmcManifestComponent> Components { get; set; }
    public int FormatVersion { get; set; }

    public static MmcManifest FromJson(string json) => JsonSerializer.Deserialize<MmcManifest>(json, JsonOptions);
}