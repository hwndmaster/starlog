using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace Genius.Starlog.UI.Helpers;

// TODO: Not used yet
[ExcludeFromCodeCoverage]
public static class PrincipalHelper
{
    private static bool? _isElevated;

    public static bool IsElevated()
    {
        if (_isElevated is null)
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                _isElevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                _isElevated = false;
            }
        }

        return _isElevated.Value;
    }
}
