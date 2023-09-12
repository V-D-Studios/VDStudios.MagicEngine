using VDStudios.MagicEngine.Graphics;

namespace Veldrid.OpenGL.NoAllocEntryList;

internal struct NoAllocClearColorTargetEntry
{
    public readonly uint Index;
    public readonly RgbaVector ClearColor;

    public NoAllocClearColorTargetEntry(uint index, RgbaVector clearColor)
    {
        Index = index;
        ClearColor = clearColor;
    }
}