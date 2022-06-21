using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// An assorted array of extension methods, helper and shortcut methods, and misc static properties
/// </summary>
public static class AssortedHelpers
{
    /// <summary>
    /// Represents a <see cref="Task"/> that returns <c>true</c>
    /// </summary>
    public static readonly Task<bool> TrueResult = Task.FromResult(true);

    /// <summary>
    /// Represents a <see cref="Task"/> that returns <c>false</c>
    /// </summary>
    public static readonly Task<bool> FalseResult = Task.FromResult(false);

}
