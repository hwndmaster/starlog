namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

internal readonly record struct ParsedLine(string DateTime, string Level, ParsedFieldValue[] Fields, string Message);
