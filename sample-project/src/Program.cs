using SampleProject.Commands;

namespace SampleProject;

public class Program
{
    public static Task Main(string[] args) => ApiCommand.RunAsync(args);
}
