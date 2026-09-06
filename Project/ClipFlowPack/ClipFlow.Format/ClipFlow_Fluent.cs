using FluentCommandLine;

namespace ClipFlow.Format;

public class ClipFlow_Fluent
{
    public record help_message(string message);
    
    public static void FluentModuleInitialize(FluentEnvironment env)
    {
        env.AddModule<ClipType>();
        env.AddModule<ClipEndpoint>();
    }
    
    // git ls-files | ClipFlow copy files console
    
    [FluentMethod("-help")]
    [KV_FA(FluentAttribute.Help, "show this help output")]
    public static ClipCommand help()
    {
        //access outside the command, fetching FluentEnvironment while still current
        FluentEnvironment env = FluentEnvironment.Current;
        
        return new ClipDelegateCommand((ctx) =>
        {
           env.Context.TryGet(out help_message? message);
           if (message != null) ctx.ErrorOutput.WriteLine(message.message);
           
           ctx.ErrorOutput.WriteLine(env.Help());

        });
    }
    
    public class ShowTopic : ClipDelegateCommand
    {
        public ShowTopic(Action<ClipContext> action) : base(action)
        {
        }
    }

    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "show topic information")]
    public static ClipCommand show(ShowTopic topic)
    {
        return topic;
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "show topic information")]
    public static ShowTopic avail()
    {
        return new ShowTopic((ctx) =>
        {
            ClipUtilityWindow.ShowAvail(ctx);
        });
    }
    
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "apply clipboard content to an endpoint")]
    public static ClipCommand paste(ClipType type, ClipEndpoint endpoint)
    {
        //access outside the command, fetching FluentEnvironment while still current
        FluentEnvironment env = FluentEnvironment.Current;
        
        return new ClipDelegateCommand((ctx) =>
        {
            ClipUtilityWindow.Paste(ctx, type, endpoint);
        });
    }
    
    [FluentMethod]
    [KV_FA(FluentAttribute.Help, "set clipboard content from an endpoint")]
    public static ClipCommand copy(ClipType type, ClipEndpoint endpoint)
    {
        //access outside the command, fetching FluentEnvironment while still current
        FluentEnvironment env = FluentEnvironment.Current;
        
        return new ClipDelegateCommand((ctx) =>
        {
            ClipUtilityWindow.Copy(ctx, type, endpoint);
        });
    }
        
}