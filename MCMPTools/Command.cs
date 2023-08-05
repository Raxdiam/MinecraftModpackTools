namespace MCMPTools;

public abstract class Command
{
    protected Command(string name)
    {
        Name = name;
    }

    protected CommandManager Manager { get; private set; }

    public string Name { get; }

    public abstract Task RunAsync(string[] args);

    internal void SetManager(CommandManager manager) => Manager = manager;
}