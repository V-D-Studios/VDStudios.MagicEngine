using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.Veldrid;

/// <summary>
/// Contains a set of extensions for <see cref="GraphicsDevice"/>
/// </summary>
public static class GraphicsDeviceExtensions
{
    /// <summary>
    /// An asynchronous method that returns when all submitted <see cref="CommandList"/> objects have fully completed.
    /// </summary>
    /// <param name="gd">The <see cref="GraphicsDevice"/> instance to perform on</param>
    public static Task WaitForIdleAsync(this GraphicsDevice gd) => Task.Run(() => gd.WaitForIdle());
}
