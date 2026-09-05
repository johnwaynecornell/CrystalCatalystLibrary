// See https://aka.ms/new-console-template for more information

using FluentCommandLine;
using ClipFlow.Format;

FluentEnvironment env = new FluentEnvironment();
env.AddModule<ClipFlow_Fluent>();

env.Context.Set(new ClipFlow_Fluent.help_message(@"Usage: ClipFlow [arguments]

ClipFlow is under active development.

The command language and available commands may grow between releases.
For this build, the generated help is the authoritative reference.
"));

List<String> cl = new List<String>(args);

cl = new List<string>("-help".Split(' '));

env.ServeTypes = new Type[] { typeof(ClipCommand) };

int cl_index = 0;

ClipCommand? clipCommand = null;

FluentMethodRegistry.RegistryParseResult commandResult = null;

while (cl_index < cl.Count)
{
    var res = env.ParseOne(cl, ref cl_index);
    if (env.WantExit)
        break;

    if (res == null)
    {
        if (env.Status == 0)
            Console.Error.WriteLine($"Unrecognized command or argument at index {cl_index}: '{cl[cl_index]}'");
        break;
    }

    if (res.Result != null)
    {
        Object Result = res.Result;
        Type T = Result.GetType();
        if (Result is ClipCommand command)
        {
            env.Unique(ref clipCommand, command, () => "Only one command allowed");
            commandResult = res;
        }
    }
}

if (cl_index < cl.Count && !env.WantExit)
{
    Console.WriteLine($"Unconsumed arguments remaining at index {cl_index}: {string.Join(" ", cl.GetRange(cl_index, cl.Count - cl_index))}");
}

if (env.WantExit || env.Status != 0)return env.Status == 0 ? 0 : 1;

if (commandResult.Result is ClipCommand cmd) cmd.Execute(new());

return 0;


