namespace MCMPTools.Models.MMC;

public class MmcManifestComponent
{
    public string CachedName { get; set; }
    public List<MmcManifestComponentRequire> CachedRequires { get; set; }
    public string CachedVersion { get; set; }
    public bool CachedVolatile { get; set; }
    public bool DependencyOnly { get; set; }
    public bool Important { get; set; }
    public string Uid { get; set; }
    public string Version { get; set; }
}