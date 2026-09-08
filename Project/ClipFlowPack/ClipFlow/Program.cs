// See https://aka.ms/new-console-template for more information

using System.Text;
using FluentCommandLine;
using ClipFlow.Format;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

FluentEnvironment env = new FluentEnvironment();
env.AddModule<ClipFlow_Fluent>();

env.Context.Set(new ClipFlow_Fluent.help_message(@"Usage: ClipFlow [arguments]

ClipFlow is under active development.

The command language and available commands may grow between releases.
For this build, the generated help is the authoritative reference.
"));

bool diag = false;

env.EnsureRegistry(typeof(option), "options");
env.Registries[typeof(option)].AddSource(() =>
{
    diag = true;
} , "-diag", "show diagnostic messages", new string[]{},new string[]{}, new string?[]{});

List<String> cl;

cl = new List<string> { "paste","text", "console" };
cl = new List<string> { "paste","image", "console" };
cl = new List<string> { "paste","text", "console" };
cl = new List<string> { "show","avail" };
cl = new List<string> { "copy","text", "string", "Hello World" };

cl = new List<String>(args);

env.ServeTypes = new Type[] { typeof(ClipCommand), typeof(option) };

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

ClipContext ctx = new ();
ctx.Diagnostic = (context, msg) =>
{
    if (diag) context.ErrorOutput.WriteLine($"DIAG: {msg}");
}; 

ClipCommand cmd;

if (commandResult == null)
{
    using (FluentEnvironmentScope.Enter(env))
    {
        cmd = ClipFlow_Fluent.help();
    }
}
else cmd = (ClipCommand)commandResult.Result;

cmd.Execute(ctx);

return ctx.Status == 0 ? 0 : 1;

record option();

record diag_option() : option;