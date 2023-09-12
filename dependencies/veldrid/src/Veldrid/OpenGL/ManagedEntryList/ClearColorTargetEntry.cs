using VDStudios.MagicEngine.Graphics;

namespace Veldrid.OpenGL.ManagedEntryList;

internal class ClearColorTargetEntry : OpenGLCommandEntry
{
    public uint Index;
    public RgbaVector ClearColor;

    public ClearColorTargetEntry(uint index, RgbaVector clearColor)
    {
        Index = index;
        ClearColor = clearColor;
    }

    public ClearColorTargetEntry() { }

    public ClearColorTargetEntry Init(uint index, RgbaVector clearColor)
    {
        Index = index;
        ClearColor = clearColor;
        return this;
    }

    public override void ClearReferences()
    {
    }
}