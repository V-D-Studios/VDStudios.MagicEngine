using Veldrid;

namespace VDStudios.MagicEngine.Internal;

internal readonly record struct RenderTargetState(IRenderTarget Target, Framebuffer ActiveBuffer, DrawParameters Parameters);
