using System;
using System.Runtime.Versioning;
using Vortice.Direct3D11;

namespace Veldrid.D3D11;

[SupportedOSPlatform("Windows")]
internal class D3D11TextureView : TextureView
{
    private string _name;
    private bool _disposed;

    public ID3D11ShaderResourceView ShaderResourceView { get; }
    public ID3D11UnorderedAccessView UnorderedAccessView { get; }

    public D3D11TextureView(D3D11GraphicsDevice gd, ref TextureViewDescription description)
        : base(ref description)
    {
        ID3D11Device device = gd.Device;
        D3D11Texture d3dTex = Util.AssertSubtype<Texture, D3D11Texture>(description.Target);
        description.GetData(out var format, out var arrayLayers, out var baseMipLevel, out var mipLevels, out var baseArrayLayer);
        ShaderResourceViewDescription srvDesc = D3D11Util.GetSrvDesc(
            d3dTex,
            baseMipLevel,
            mipLevels,
            baseArrayLayer,
            arrayLayers,
            format);
        ShaderResourceView = device.CreateShaderResourceView(d3dTex.DeviceTexture, srvDesc);

        if ((d3dTex.Usage & TextureUsage.Storage) == TextureUsage.Storage)
        {
            UnorderedAccessViewDescription uavDesc = new UnorderedAccessViewDescription();
            uavDesc.Format = D3D11Formats.GetViewFormat(d3dTex.DxgiFormat);

            if ((d3dTex.Usage & TextureUsage.Cubemap) == TextureUsage.Cubemap)
            {
                throw new NotSupportedException();
            }
            else if (d3dTex.Depth == 1)
            {
                if (d3dTex.ArrayLayers == 1)
                {
                    if (d3dTex.Type == TextureType.Texture1D)
                    {
                        uavDesc.ViewDimension = UnorderedAccessViewDimension.Texture1D;
                        uavDesc.Texture1D.MipSlice = (int)baseMipLevel;
                    }
                    else
                    {
                        uavDesc.ViewDimension = UnorderedAccessViewDimension.Texture2D;
                        uavDesc.Texture2D.MipSlice = (int)baseMipLevel;
                    }
                }
                else
                {
                    if (d3dTex.Type == TextureType.Texture1D)
                    {
                        uavDesc.ViewDimension = UnorderedAccessViewDimension.Texture1DArray;
                        uavDesc.Texture1DArray.MipSlice = (int)baseMipLevel;
                        uavDesc.Texture1DArray.FirstArraySlice = (int)baseArrayLayer;
                        uavDesc.Texture1DArray.ArraySize = (int)arrayLayers;
                    }
                    else
                    {
                        uavDesc.ViewDimension = UnorderedAccessViewDimension.Texture2DArray;
                        uavDesc.Texture2DArray.MipSlice = (int)baseMipLevel;
                        uavDesc.Texture2DArray.FirstArraySlice = (int)baseArrayLayer;
                        uavDesc.Texture2DArray.ArraySize = (int)arrayLayers;
                    }
                }
            }
            else
            {
                uavDesc.ViewDimension = UnorderedAccessViewDimension.Texture3D;
                uavDesc.Texture3D.MipSlice = (int)baseMipLevel;

                // Map the entire range of the 3D texture.
                uavDesc.Texture3D.FirstWSlice = 0;
                uavDesc.Texture3D.WSize = (int)d3dTex.Depth;
            }

            UnorderedAccessView = device.CreateUnorderedAccessView(d3dTex.DeviceTexture, uavDesc);
        }
    }

    public override string Name
    {
        get => _name;
        set
        {
            _name = value;
            if (ShaderResourceView != null)
            {
                ShaderResourceView.DebugName = value + "_SRV";
            }
            if (UnorderedAccessView != null)
            {
                UnorderedAccessView.DebugName = value + "_UAV";
            }
        }
    }

    public override bool IsDisposed => _disposed;

    public override void Dispose()
    {
        if (!_disposed)
        {
            ShaderResourceView?.Dispose();
            UnorderedAccessView?.Dispose();
            _disposed = true;
        }
    }
}
