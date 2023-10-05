using System.Reactive.Linq;

namespace Genius.Starlog.UI;

// TODO: Cover with unit tests
public static class ObservableExtensions
{
    public static IObservable<bool> OnOneTimeExecutedBooleanAction(this IActionCommand actionCommand)
    {
        return actionCommand.Executed
            .Where(x => x)
            .Take(1);
    }
}
