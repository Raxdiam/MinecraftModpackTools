using System.Text.Json.Serialization;

namespace MCMPTools.Models.Curse;

public class CurseManifestFile
{
    [JsonPropertyName("projectID")]
    public int ProjectId { get; set; }
    [JsonPropertyName("fileID")]
    public int FileId { get; set; }
    public bool Required { get; set; }
}