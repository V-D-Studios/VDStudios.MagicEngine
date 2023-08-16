using VDStudios.MagicEngine.Graphics;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Internal;

internal readonly record struct RenderTargetState(IRenderTarget Target, Framebuffer ActiveBuffer, DrawParameters Parameters);
