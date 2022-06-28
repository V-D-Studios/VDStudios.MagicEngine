using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine;

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

    /// <summary>
    /// Updates a <see cref="DeviceBuffer"/> region with new data that may be stack allocated. This method must use a blittable value type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The blittable value type to update the buffer with</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/> to update <paramref name="buffer"/> with</param>
    /// <param name="buffer">The resource to update</param>
    /// <param name="data">A <see cref="Span{T}"/> containing the data to upload</param>
    /// <param name="bufferOffsetInBytes">An offset, in bytes, from the beginning of <paramref name="buffer"/>'s storage, at which new data will be uploaded</param>
    public static void UpdateBuffer<T>(this GraphicsDevice device, DeviceBuffer buffer, uint bufferOffsetInBytes, Span<T> data) where T : unmanaged
    {
        device.UpdateBuffer(buffer, bufferOffsetInBytes, ref data[0], SizeOf<T>.By((uint)data.Length));
    }
}
