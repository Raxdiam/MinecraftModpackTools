namespace MCMPTools.Commands;

internal class PurgeCommand : Command
{
    public PurgeCommand() : base("purge") { }
    
    /// <param name="args">First parameter should be the instance directory</param>
    public override Task RunAsync(string[] args)
    {
        if (!Util.ValidateInstanceDirectory(args[0]))
            throw new CommandException("Invalid instance directory");

        var indexData = Util.GetIndexData(args[0]);
        var modsDir = Path.Combine(args[0], ".minecraft", "mods");

        var untracked = Directory.GetFiles(modsDir)
            .Select(f => f.EndsWith(".disabled") ? f[..^9] : f)
            .Where(f => indexData.All(x => x.FileName != Path.GetFileName(f)))
            .ToList();

        if (untracked.Count == 0)
            return Task.CompletedTask;

        Console.WriteLine($"Found {untracked.Count} untracked mods:");
        foreach (var file in untracked) {
            Console.WriteLine($"- {Path.GetFileName(file)}");
        }

        Console.Write("Purge? (y/n): ");
        Console.WriteLine();
        var key = Console.ReadKey();
        if (key.Key != ConsoleKey.Y) return Task.CompletedTask;

        Console.Write($"Purging {untracked.Count} untracked mods... ");
        foreach (var file in untracked) {
            File.Delete(file);
        }
        Console.WriteLine("Done");

        var verifiedDeleted = 0;
        var failed = new List<string>();
        foreach (var file in untracked) {
            if (File.Exists(file)) {
                failed.Add(file);
                continue;
            }
            verifiedDeleted++;
        }

        if (verifiedDeleted > 0) {
            Console.WriteLine($"Successfully purged {verifiedDeleted} untracked mods");
        }
        else {
            Console.WriteLine("Failed to purge untracked mods:");
            foreach (var file in failed) {
                Console.WriteLine($"- {Path.GetFileName(file)}");
            }
        }
        
        return Task.CompletedTask;
    }
}