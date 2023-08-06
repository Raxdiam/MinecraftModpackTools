namespace MCMPTools;

public class IndexFile
{
    public string FileName { get; set; }
    public bool Disabled { get; set; }
    public IndexFileCurse Curse { get; set; }
    public IndexFileModrinth Modrinth { get; set; }
}

public class IndexFileCurse
{
    public uint FileId { get; set; }
    public uint ProjectId { get; set; }
}

public class IndexFileModrinth
{
    public string ModId { get; set; }
    public string Version { get; set; }
}

public enum HashFormat
{
    Sha1,
    Sha256,
    Sha512
}