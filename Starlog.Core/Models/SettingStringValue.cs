namespace Genius.Starlog.Core.Models;

/// <summary>
///   Defines a named string value.
/// </summary>
/// <param name="Name">A name.</param>
/// <param name="Value">A value.</param>
public sealed record SettingStringValue(string Name, string Value);
