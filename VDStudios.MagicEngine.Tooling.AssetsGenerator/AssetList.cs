using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static VDStudios.MagicEngine.Tooling.AssetsGenerator.Helper;

namespace VDStudios.MagicEngine.Tooling.AssetsGenerator;

internal sealed class AssetGroup
{
    public string GroupName
    {
        get => PropGet(ref gn)!;
        init => PropSet(ref gn, value);
    }
    private string? gn;

    public AssetDescription[] Assets { get; init; } = Array.Empty<AssetDescription>();
}

internal sealed class AssetDescription
{

    public string File
    {
        get => PropGet(ref fn)!;
        init => PropSet(ref fn, value);
    }
    string? fn;

    public string Name
    {
        get => n ??= FileToName(File);
        init => n = value;
    }
    string? n;

    public string ReadAs
    {
        get => ReadAsValue.ToString();
        init => ReadAsValue = Enum.Parse<ReadAs>(value);
    }

    internal ReadAs ReadAsValue { get; init; }
}

public enum ReadAs
{
    RawBytes,
    UTF8Text
}