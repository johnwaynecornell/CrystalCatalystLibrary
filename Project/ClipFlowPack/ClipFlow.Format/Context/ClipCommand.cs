namespace ClipFlow.Format;

///<summary>
/// This type is meant as a base class for commands that can be executed by the farm.
/// It is used as the FluentMethod return value for an executable unit.
/// </summary> 

public abstract class ClipCommand
{
    public abstract void Execute(ClipContext context);
}
