using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// <param name="data">A <see cref="ReadOnlySpan{T}"/> containing the data to upload</param>
    /// <param name="bufferOffsetInBytes">An offset, in bytes, from the beginning of <paramref name="buffer"/>'s storage, at which new data will be uploaded</param>
    public unsafe static void UpdateBuffer<T>(this GraphicsDevice device, DeviceBuffer buffer, uint bufferOffsetInBytes, Span<T> data) where T : unmanaged 
        => UpdateBuffer(device, buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)data);

    /// <summary>
    /// Updates a <see cref="DeviceBuffer"/> region with new data that may be stack allocated. This method must use a blittable value type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The blittable value type to update the buffer with</typeparam>
    /// <param name="commandList">The <see cref="CommandList"/> to update <paramref name="buffer"/> with</param>
    /// <param name="buffer">The resource to update</param>
    /// <param name="data">A <see cref="ReadOnlySpan{T}"/> containing the data to upload</param>
    /// <param name="bufferOffsetInBytes">An offset, in bytes, from the beginning of <paramref name="buffer"/>'s storage, at which new data will be uploaded</param>
    public unsafe static void UpdateBuffer<T>(this CommandList commandList, DeviceBuffer buffer, uint bufferOffsetInBytes, Span<T> data) where T : unmanaged 
        => UpdateBuffer(commandList, buffer, bufferOffsetInBytes, (ReadOnlySpan<T>)data);

    /// <summary>
    /// Updates a <see cref="DeviceBuffer"/> region with new data that may be stack allocated. This method must use a blittable value type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The blittable value type to update the buffer with</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/> to update <paramref name="buffer"/> with</param>
    /// <param name="buffer">The resource to update</param>
    /// <param name="data">A <see cref="ReadOnlySpan{T}"/> containing the data to upload</param>
    /// <param name="bufferOffsetInBytes">An offset, in bytes, from the beginning of <paramref name="buffer"/>'s storage, at which new data will be uploaded</param>
    public unsafe static void UpdateBuffer<T>(this GraphicsDevice device, DeviceBuffer buffer, uint bufferOffsetInBytes, ReadOnlySpan<T> data) where T : unmanaged
    {
        fixed (T* d = data)
            device.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)d, (uint)(Unsafe.SizeOf<T>() * data.Length));
    }

    /// <summary>
    /// Updates a <see cref="DeviceBuffer"/> region with new data that may be stack allocated. This method must use a blittable value type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The blittable value type to update the buffer with</typeparam>
    /// <param name="commandList">The <see cref="CommandList"/> to update <paramref name="buffer"/> with</param>
    /// <param name="buffer">The resource to update</param>
    /// <param name="data">A <see cref="ReadOnlySpan{T}"/> containing the data to upload</param>
    /// <param name="bufferOffsetInBytes">An offset, in bytes, from the beginning of <paramref name="buffer"/>'s storage, at which new data will be uploaded</param>
    public unsafe static void UpdateBuffer<T>(this CommandList commandList, DeviceBuffer buffer, uint bufferOffsetInBytes, ReadOnlySpan<T> data) where T : unmanaged
    {
        fixed (T* d = data)
            commandList.UpdateBuffer(buffer, bufferOffsetInBytes, (IntPtr)d, (uint)(Unsafe.SizeOf<T>() * data.Length));
    }
}
