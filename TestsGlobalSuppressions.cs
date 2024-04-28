// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// SonarLint-related analyzers cannot be disabled in .editorconfig file, therefore disabling them here.

[assembly: SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed", Justification = "<Pending>")]
[assembly: SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "<Pending>")]
[assembly: SuppressMessage("Major Code Smell", "S6562:Always set the \"DateTimeKind\" when creating new \"DateTime\" instances", Justification = "<Pending>")]
