using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using VDStudios.MagicEngine.Tooling.AssetsGenerator;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

Regex csprojRegex = new(@"^.+\.csproj", RegexOptions.Compiled);

var assetsProj = args[0];

if (assetsProj is null || !csprojRegex.IsMatch(assetsProj)) 
    throw new InvalidOperationException($"Must provide a the path to a .csproj file (Whether it exists or not) as the first argument");

var assetsDir = Path.GetDirectoryName(assetsProj) ?? throw new InvalidOperationException($"{assetsProj} does not have a valid parent directory");
Environment.CurrentDirectory = assetsDir;

if (File.Exists(".lastmod"))
{
    var t = File.ReadAllText(".lastmod");
    var dat = long.Parse(t);
    if (DateTime.FromBinary(dat) >= Directory.GetLastWriteTimeUtc("."))
        return;
}

var jt = Task.Run(async () =>
{
    List<AssetGroup> groups = new();
    await Parallel.ForEachAsync(
        Directory.GetFiles(assetsDir, "*.assetgroup"),
        CancellationToken.None,
        async (item, ct) =>
        {
            var g = JsonSerializer.Deserialize<AssetGroup>(await File.ReadAllTextAsync(item, ct));
            lock (groups)
                groups.Add(g!);
        }
    );
    return groups;
});

XmlDocument d;

var csprojCreation = Task.Run(async () =>
{
    d = new();
    d.AppendChild(d.CreateComment("VDStudios.MagicEngine.Tooling.AssetsGenerator, written in 2022 by Diego García"));
    d.AppendChild(d.CreateComment("This .csproj was created with a code generation tool and any changes made WILL be discarded and overwritten."));

    var pg = d.CreateElement("PropertyGroup");
    d.AppendChild(pg);
    {
        var fw = d.CreateElement("TargetFramework");
        pg.AppendChild(fw);
        fw.Value = "net6.0";

        var iu = d.CreateElement("ImplicitUsings");
        pg.AppendChild(iu);
        fw.Value = "disable";

        var nc = d.CreateElement("Nullable");
        pg.AppendChild(nc);
        fw.Value = "enable";

        var ub = d.CreateElement("AllowUnsafeBlocks");
        pg.AppendChild(ub);
        fw.Value = "true";
    }

    var ig = d.CreateElement("ItemGroup");
    d.AppendChild(ig);
    {
        var groups = await jt;
        foreach (var g in groups.SelectMany(x => x.Assets))
        {
            var res = d.CreateElement("EmbeddedResource");
            res.SetAttribute("Include", g.File);
            ig.AppendChild(res);
        }
    }

    d.Save(assetsProj);
});

var codeGeneration = Task.Run(async () =>
{
    var directories = new List<CodeDir>();
    var groups = await jt;
    var tasks =  Parallel.ForEachAsync(groups, async (group, ct) =>
    {
        var dir = new CodeDir(group.GroupName);
        await Parallel.ForEachAsync(group.Assets, async (item, ct) =>
        {
            await Task.Yield();

        });
    });
    Directory.Delete("src", true);
    await tasks;
});

async Task GenForString(AssetDescription desc)
{

}

async Task GenForFile(AssetDescription desc)
{

}

await csprojCreation;
await codeGeneration;

File.WriteAllText(".lastmod", (DateTime.UtcNow + TimeSpan.FromSeconds(1)).ToBinary().ToString());

public static class ClassName
{
    public string TextName
}

//typeof(Program).Assembly.GetManifestResourceStream(name)

struct CodeDir
{
    public readonly string DirectoryName;
    public readonly List<CodeFile> CodeFiles;

    public CodeDir(string dirname)
    {
        DirectoryName = dirname;
        CodeFiles = new();
    }
}

struct CodeFile
{
    public readonly string FileName;
    public readonly string Source;

    public CodeFile(string fileName, string source)
    {
        FileName = fileName;
        Source = source;
    }
}