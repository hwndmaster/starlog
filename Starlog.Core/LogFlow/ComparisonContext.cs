using System.Collections.Immutable;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.LogFlow;

public sealed record ComparisonContext(Profile Profile1, ILogContainer LogContainer1, Profile Profile2, ILogContainer LogContainer2, ImmutableArray<ComparisonRecord> Records);
public readonly record struct ComparisonRecord(LogRecord? Record1, LogRecord? Record2);
