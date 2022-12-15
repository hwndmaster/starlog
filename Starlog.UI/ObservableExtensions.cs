using System.Reactive.Linq;

namespace Genius.Starlog.UI;

public static class ObservableExtensions
{
    public static IObservable<bool> OnOneTimeExecutedBooleanAction(this IActionCommand actionCommand)
    {
        return actionCommand.Executed
            .Where(x => x)
            .Take(1);
    }
}
