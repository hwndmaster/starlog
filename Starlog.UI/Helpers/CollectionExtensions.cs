namespace Genius.Starlog.UI.Helpers;

public static class CollectionExtensions
{
    public static void ReplaceItems<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
