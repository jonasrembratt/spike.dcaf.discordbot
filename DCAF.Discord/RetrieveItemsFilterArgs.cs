namespace DCAF.Discord;

public sealed class RetrieveItemsFilterArgs<T> : RetrieveItemsArgs<T>
{
    public RetrieveItemsBehavior Behavior { get; }

    public static RetrieveItemsFilterArgs<T> ContinueWith(T[] items) => new(items, RetrieveItemsBehavior.Continue);
        
    public static RetrieveItemsFilterArgs<T> EndWith(T[] items) => new(items, RetrieveItemsBehavior.End);
        
    RetrieveItemsFilterArgs(T[] items, RetrieveItemsBehavior behavior) 
        : base(items)
    {
        Behavior = behavior;
    }
}