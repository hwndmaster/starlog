// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// SonarLint-related analyzers cannot be disabled in .editorconfig file, therefore disabling them here.

[assembly: SuppressMessage("Blocker Code Smell", "S3060:\"is\" should not be used with \"this\"", Justification = "<Pending>")]
[assembly: SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "<Pending>")]
[assembly: SuppressMessage("Minor Code Smell", "S6605:Collection-specific \"Exists\" method should be used instead of the \"Any\" extension", Justification = "<Pending>")]
