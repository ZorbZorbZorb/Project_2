﻿// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Not Unity compatible", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "Not Unity compatible", Scope = "module")]
[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Often warns for Unity methods in use", Scope = "module")]