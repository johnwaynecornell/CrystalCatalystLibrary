namespace ClipFlow.Format;

public class ClipDelegateCommand : ClipCommand
{
    public Action<ClipContext> Action;
        
    public ClipDelegateCommand(Action<ClipContext> action)
    {
        Action = action;
    }
        
    public override void Execute(ClipContext context)
    {
        Action(context);
    }
}
