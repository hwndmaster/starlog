using System.Reactive;
using System.Reactive.Subjects;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

internal interface IProfileLoader
{
    Task<ProfileStateBase?> LoadProfileAsync(Profile profile, ILogContainerWriter logContainer);
    IDisposable StartProfileMonitoring(ProfileStateBase profile, ILogContainerWriter logContainer, Subject<Unit> unknownChangesDetectedSubject);
}
