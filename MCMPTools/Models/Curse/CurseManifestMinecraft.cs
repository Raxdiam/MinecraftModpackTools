namespace MCMPTools.Models.Curse;

public class CurseManifestMinecraft
{
    public string Version { get; set; }
    public List<CurseManifestMinecraftModLoader> ModLoaders { get; set; }
}