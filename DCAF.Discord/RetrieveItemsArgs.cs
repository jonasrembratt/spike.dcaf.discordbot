namespace DCAF.Discord;

public class RetrieveItemsArgs<T>
{
    public T[] Items { get; set; }

    protected RetrieveItemsArgs(T[] items)
    {
        Items = items;
    }
}