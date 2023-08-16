using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Internal;
/// <summary>
/// A modified version of Veldrid.ImGui's ImGuiRenderer.
/// Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
/// </summary>
public class ImGuiController<TGraphicsContext> : IDisposable
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private GraphicsDevice _gd;

    // Veldrid objects
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private DeviceBuffer _projMatrixBuffer;
    private Texture _fontTexture;
    private TextureView _fontTextureView;
    private Shader[] _shaders;
    private ResourceLayout _layout;
    private ResourceLayout _textureLayout;
    private Pipeline _pipeline;
    private ResourceSet _mainResourceSet;
    private ResourceSet _fontTextureResourceSet;

    private readonly nint _fontAtlasID = 1;
    private bool _controlDown;
    private bool _shiftDown;
    private bool _altDown;
    private bool _winKeyDown;

    private int _windowWidth;
    private int _windowHeight;
    private Vector2 _scaleFactor = Vector2.One;

    internal static readonly object SyncImGUI = new();

    // Image trackers
    private readonly Dictionary<TextureView, ResourceSetInfo> _setsByView = new();
    private readonly Dictionary<Texture, TextureView> _autoViewsByTexture = new();
    private readonly Dictionary<nint, ResourceSetInfo> _viewsById = new();
    private readonly List<IDisposable> _ownedResources = new();
    private int _lastAssignedID = 100;
    private readonly nint Context;

    private sealed class ImGuiControllerLockRelease : IDisposable
    {
        private readonly ImGuiController Controller;

        public ImGuiControllerLockRelease(ImGuiController controller)
        {
            Controller = controller;
        }

        public bool lockTaken;
        public void Enter()
        {
            Monitor.Enter(SyncImGUI, ref lockTaken);
            ImGui.SetCurrentContext(Controller.Context);
            ImGui.NewFrame();
        }

        public void Exit()
        {
            ImGui.EndFrame();
            if (lockTaken)
            {
                lockTaken = false;
                Monitor.Exit(SyncImGUI);
            }
        }

        void IDisposable.Dispose() => Exit();
    }

    /// <summary>
    /// Constructs a new ImGuiController.
    /// </summary>
    public ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, int width, int height)
    {
        lock (SyncImGUI)
        {
            _gd = gd;
            _windowWidth = width;
            _windowHeight = height;

            Context = ImGui.CreateContext();
            ImGui.SetCurrentContext(Context);
            var fonts = ImGui.GetIO().Fonts;
            ImGui.GetIO().Fonts.AddFontDefault();
            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            CreateDeviceResources(gd, outputDescription);
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f);
        }

        lockRelease = new(this);
    }

    private readonly ImGuiControllerLockRelease lockRelease;

    /// <summary>
    /// Begins a new Frame and locks ImGUI from multi-threaded access
    /// </summary>
    /// <remarks>
    /// **ALWAYS** wrap the the returned <see cref="IDisposable"/> in an using statement. Failing to dispose of it (releasing the lock) WILL result in a deadlock for all <see cref="GraphicsManager"/>s that use ImGUI in any given frame
    /// </remarks>
    /// <returns>
    /// An object that, when disposed, will release ImGUI and End the frame
    /// </returns>
    public IDisposable Begin()
    {
        lockRelease.Enter();
        return lockRelease;
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void DestroyDeviceObjects()
    {
        Dispose();
    }

    public void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription)
    {
        _gd = gd;
        ResourceFactory factory = gd.ResourceFactory;
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        _indexBuffer.Name = "ImGui.NET Index Buffer";
        RecreateFontDeviceTexture(gd);

        _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

        byte[] vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "Vertex", ShaderStages.Vertex);
        byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "Fragment", ShaderStages.Fragment);
        _shaders = new Shader[2];
        _shaders[0] = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "VS" : "main"));
        _shaders[1] = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "FS" : "main"));

        VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
        {
                new VertexLayoutDescription(
                    new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
        };

        _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

        _pipeline = factory.CreateGraphicsPipeline(new(
            BlendStateDescription.SingleAlphaBlend,
            new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(vertexLayouts, _shaders),
            new ResourceLayout[] { _layout, _textureLayout },
            outputDescription,
            ResourceBindingModel.Improved)
        );

        _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
            _projMatrixBuffer,
            gd.PointSampler));

        _fontTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTextureView));
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public nint GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
    {
        if (!_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
        {
            ResourceSet resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));
            rsi = new ResourceSetInfo(GetNextImGuiBindingID(), resourceSet);

            _setsByView.Add(textureView, rsi);
            _viewsById.Add(rsi.ImGuiBinding, rsi);
            _ownedResources.Add(resourceSet);
        }

        return rsi.ImGuiBinding;
    }

    private nint GetNextImGuiBindingID()
    {
        int newID = _lastAssignedID++;
        return newID;
    }

    /// <summary>
    /// Gets or creates a handle for a texture to be drawn with ImGui.
    /// Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public nint GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
    {
        if (!_autoViewsByTexture.TryGetValue(texture, out var textureView))
        {
            textureView = factory.CreateTextureView(texture);
            _autoViewsByTexture.Add(texture, textureView);
            _ownedResources.Add(textureView);
        }

        return GetOrCreateImGuiBinding(factory, textureView);
    }

    /// <summary>
    /// Retrieves the shader texture binding for the given helper handle.
    /// </summary>
    public ResourceSet GetImageResourceSet(nint imGuiBinding)
    {
        return !_viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo tvi)
            ? throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString())
            : tvi.ResourceSet;
    }

    public void ClearCachedImageResources()
    {
        foreach (IDisposable resource in _ownedResources)
        {
            resource.Dispose();
        }

        _ownedResources.Clear();
        _setsByView.Clear();
        _viewsById.Clear();
        _autoViewsByTexture.Clear();
        _lastAssignedID = 100;
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public void RecreateFontDeviceTexture(GraphicsDevice gd)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        // Build
        io.Fonts.GetTexDataAsRGBA32(out nint pixels, out int width, out int height, out int bytesPerPixel);
        // Store our identifier
        io.Fonts.SetTexID(_fontAtlasID);

        _fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled));
        _fontTexture.Name = "ImGui.NET Font Texture";
        gd.UpdateTexture(
            _fontTexture,
            pixels,
            (uint)(bytesPerPixel * width * height),
            0,
            0,
            0,
            (uint)width,
            (uint)height,
            1,
            0,
            0);
        _fontTextureView = gd.ResourceFactory.CreateTextureView(_fontTexture);

        io.Fonts.ClearTexData();
    }

    private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
    {
        switch (factory.BackendType)
        {
            case GraphicsBackend.Direct3D11:
                {
                    string resourceName = name + "HLSLBytes";
                    return (byte[])ImGuiResources.ResourceManager.GetObject(resourceName)!;
                }
            case GraphicsBackend.OpenGL:
                {
                    string resourceName = name + "GLSL";
                    return (byte[])ImGuiResources.ResourceManager.GetObject(resourceName)!;
                }
            case GraphicsBackend.Vulkan:
                {
                    string resourceName = name + "SPIRV";
                    return (byte[])ImGuiResources.ResourceManager.GetObject(resourceName)!;
                }
            case GraphicsBackend.Metal:
                {
                    string resourceName = name + "METAL";
                    return (byte[])ImGuiResources.ResourceManager.GetObject(resourceName)!;
                }
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// This method requires a <see cref="GraphicsDevice"/> because it may create new DeviceBuffers if the size of vertex
    /// or index data has increased beyond the capacity of the existing buffers.
    /// A <see cref="CommandList"/> is needed to submit drawing and resource update commands.
    /// </summary>
    public void Render(GraphicsDevice gd, CommandList cl)
    {
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData(), gd, cl);
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(float deltaSeconds, InputSnapshotBuffer snapshot)
    {
        SetPerFrameImGuiData(deltaSeconds);
        UpdateImGuiInput(snapshot);
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        io.DisplayFramebufferScale = _scaleFactor;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    private void UpdateImGuiInput(InputSnapshotBuffer snapshot)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        Vector2 mousePosition = snapshot.MousePosition;

        // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
        bool leftPressed = false;
        bool middlePressed = false;
        bool rightPressed = false;
        foreach (var me in snapshot.MouseEvents)
        {
            if (me.Pressed != 0)
            {
                switch (me.Pressed)
                {
                    case MouseButton.Left:
                        leftPressed = true;
                        break;
                    case MouseButton.Middle:
                        middlePressed = true;
                        break;
                    case MouseButton.Right:
                        rightPressed = true;
                        break;
                }
            }
        }

#if DEBUG
        var rdown = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
        var ldown = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
        var mdown = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
        io.MouseDown[0] = ldown;
        io.MouseDown[1] = rdown;
        io.MouseDown[2] = mdown;
#else
        io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
        io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
        io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
#endif
        io.MousePos = mousePosition;
        io.MouseWheel = snapshot.WheelVerticalDelta;
        io.MouseWheelH = snapshot.WheelHorizontalDelta;

        IReadOnlyList<uint> keyCharPresses = snapshot.KeyCharPresses;
        for (int i = 0; i < keyCharPresses.Count; i++)
        {
            uint c = keyCharPresses[i];
            io.AddInputCharacter(c);
        }

        foreach (var ke in snapshot.KeyEvents)
        {
            io.KeysDown[(int)ke.Scancode] = ke.IsPressed;
            if (ke.Scancode == Scancode.LeftControl)
            {
                _controlDown = ke.IsPressed;
            }
            if (ke.Scancode == Scancode.LeftShift)
            {
                _shiftDown = ke.IsPressed;
            }
            if (ke.Scancode == Scancode.LeftAlt)
            {
                _altDown = ke.IsPressed;
            }
            if (ke.Scancode == Scancode.LeftGUI)
            {
                _winKeyDown = ke.IsPressed;
            }
        }

        io.KeyCtrl = _controlDown;
        io.KeyAlt = _altDown;
        io.KeyShift = _shiftDown;
        io.KeySuper = _winKeyDown;
    }

    private static void SetKeyMappings()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Scancode.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Scancode.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Scancode.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Scancode.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Scancode.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Scancode.Pageup;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Scancode.Pagedown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Scancode.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Scancode.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Scancode.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Scancode.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Scancode.Return;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Scancode.Escape;
        io.KeyMap[(int)ImGuiKey.Space] = (int)Scancode.Space;
        io.KeyMap[(int)ImGuiKey.A] = (int)Scancode.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Scancode.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Scancode.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Scancode.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Scancode.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Scancode.Z;
    }

    private void RenderImDrawData(ImDrawDataPtr draw_data, GraphicsDevice gd, CommandList cl)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if (draw_data.CmdListsCount == 0)
        {
            return;
        }

        uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if (totalVBSize > _vertexBuffer.SizeInBytes)
        {
            gd.DisposeWhenIdle(_vertexBuffer);
            _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
        if (totalIBSize > _indexBuffer.SizeInBytes)
        {
            gd.DisposeWhenIdle(_indexBuffer);
            _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        for (int i = 0; i < draw_data.CmdListsCount; i++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

            cl.UpdateBuffer(
                _vertexBuffer,
                vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                cmd_list.VtxBuffer.Data,
                (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

            cl.UpdateBuffer(
                _indexBuffer,
                indexOffsetInElements * sizeof(ushort),
                cmd_list.IdxBuffer.Data,
                (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
            0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        _gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

        cl.SetVertexBuffer(0, _vertexBuffer);
        cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(_pipeline);
        cl.SetGraphicsResourceSet(0, _mainResourceSet);

        draw_data.ScaleClipRects(io.DisplayFramebufferScale);

        // Render command lists
        int vtx_offset = 0;
        int idx_offset = 0;
        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != nint.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (pcmd.TextureId != nint.Zero)
                    {
                        if (pcmd.TextureId == _fontAtlasID)
                        {
                            cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                        }
                        else
                        {
                            cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                        }
                    }

                    cl.SetScissorRect(
                        0,
                        (uint)pcmd.ClipRect.X,
                        (uint)pcmd.ClipRect.Y,
                        (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                        (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                    cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)pcmd.VtxOffset + vtx_offset, 0);
                }
            }
            vtx_offset += cmd_list.VtxBuffer.Size;
            idx_offset += cmd_list.IdxBuffer.Size;
        }
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _projMatrixBuffer.Dispose();
        _fontTexture.Dispose();
        _fontTextureView.Dispose();
        for (int i = 0; i < _shaders.Length; i++) _shaders[i].Dispose();
        _layout.Dispose();
        _textureLayout.Dispose();
        _pipeline.Dispose();
        _mainResourceSet.Dispose();

        foreach (IDisposable resource in _ownedResources)
        {
            resource.Dispose();
        }
    }

    private struct ResourceSetInfo
    {
        public readonly nint ImGuiBinding;
        public readonly ResourceSet ResourceSet;

        public ResourceSetInfo(nint imGuiBinding, ResourceSet resourceSet)
        {
            ImGuiBinding = imGuiBinding;
            ResourceSet = resourceSet;
        }
    }
}