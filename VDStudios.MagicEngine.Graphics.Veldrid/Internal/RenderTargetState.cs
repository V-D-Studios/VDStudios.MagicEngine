using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Internal;

internal readonly record struct RenderTargetState(RenderTarget Target, Framebuffer ActiveBuffer, DrawParameters Parameters);
