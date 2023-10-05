using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public sealed record ComparisonContext(Profile Profile1, ILogContainer LogContainer1, Profile Profile2, ILogContainer LogContainer2);
