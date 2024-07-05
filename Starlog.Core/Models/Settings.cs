using System.Collections.Immutable;

namespace Genius.Starlog.Core.Models;

/// <summary>
///   The model for all app settings.
/// </summary>
public sealed class Settings
{
    /// <summary>
    ///   Indicates whether the application should automatically load previously opened profile.
    ///   Works in conjunction with <see cref="AutoLoadProfile" />.
    /// </summary>
    public bool AutoLoadPreviouslyOpenedProfile { get; set; }

    /// <summary>
    ///   The identifier of the profile to be automatically loaded when the application starts.
    ///   Works in conjunction with <see cref="AutoLoadPreviouslyOpenedProfile" />.
    /// </summary>
    public Guid? AutoLoadProfile { get; set; }

    /// <summary>
    ///   A collection of pattern templates which can be used in profiles with log codec type "Plain Text".
    /// </summary>
    public ICollection<PatternValue> PlainTextLogCodecLinePatterns { get; set; } = Array.Empty<PatternValue>();

    /// <summary>
    ///   A collection of sorted columns on the Profiles view.
    /// </summary>
    public ProfilesViewSettings ProfilesView { get; set; } = new ProfilesViewSettings();
}

/// <summary>
///   Contains the settings for Profiles view.
/// </summary>
public sealed class ProfilesViewSettings
{
    /// <summary>
    ///   A collection of sorted columns.
    ///   Note: the string item is represented in the following format:
    ///   {ColumnName}_{true|false}
    ///   Where {true|false} - whether the column is sorted in ascending order or not.
    /// </summary>
    public ICollection<string> SortedColumns { get; set; } = [];

    public ImmutableList<(string ColumnName, bool SortAsc)> GetSortedColumns()
    {
        return SortedColumns.Select(x => {
            var arr = x.Split("_");
            return (arr[0], arr[1].Equals("true", StringComparison.OrdinalIgnoreCase));
        }).ToImmutableList();
    }

    public void SetSortedColumns(IEnumerable<(string ColumnName, bool SortAsc)> newSortedColumns)
    {
        SortedColumns = newSortedColumns.Select(x => $"{x.ColumnName}_{x.SortAsc}").ToImmutableList();
    }
}
