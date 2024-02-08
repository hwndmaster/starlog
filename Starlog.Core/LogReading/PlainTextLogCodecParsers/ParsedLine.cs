namespace Genius.Starlog.Core.LogReading.PlainTextLogCodecParsers;

internal readonly record struct ParsedLine(string DateTime, string Level, string Thread, string Logger, string Message);
