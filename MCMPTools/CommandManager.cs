
namespace MCMPTools;

public class CommandManager
{
    private readonly List<Command> _commands = new();

    public CommandManager RegisterCommand(Command command)
    {
        command.SetManager(this);
        _commands.Add(command);
        return this;
    }

    public Command GetCommad(string name) => _commands.FirstOrDefault(c => c.Name == name);

    public async Task RunAsync(string[] args)
    {
        if (args.Length == 0) {
            Console.WriteLine("No command specified.");
            return;
        }

        var command = _commands.FirstOrDefault(c => c.Name == args[0]);
        if (command == null) {
            Console.WriteLine($"Unknown command: {args[0]}");
            return;
        }

        try {
            await command.RunAsync(args[1..]);
        }
        catch (CommandException e) {
            Console.WriteLine(e);
        }

        
    }
}