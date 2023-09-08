using System.Buffers;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Provides a set of extensions to work with and load <see cref="Shader"/>s
/// </summary>
public static class ShaderExtensions
{
    /// <summary>
    /// Loads all the shaders in the given directory, Selecting their <see cref="ShaderStages"/> them by their file extension. "main" will be selected as their entry points
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <item>
    ///     <term><c>.vert.spv</c></term>
    ///     <description>Vertex Shader file</description>
    /// </item>
    /// <item>
    ///     <term><c>.tesc.spv</c></term>
    ///     <description>Tesselation control shader</description>
    /// </item>
    /// <item>
    ///     <term><c>.tese.spv</c></term>
    ///     <description>Tesselation evaluation shader</description>
    /// </item>
    /// <item>
    ///     <term><c>.geom.spv</c></term>
    ///     <description>Geometry shader</description>
    /// </item>
    /// <item>
    ///     <term><c>.frag.spv</c></term>
    ///     <description>Fragment shader</description>
    /// </item>
    /// <item>
    ///     <term><c>.comp.spv</c></term>
    ///     <description>Compute shader</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="factory">The <see cref="ResourceFactory"/> that will be used to create the shaders</param>
    /// <param name="path">The relative or absolute path to the directory to search. This string is not case-sensitive</param>
    /// <param name="searchPattern">The search string to match against the names of files in <paramref name="path"/>. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories. The default value is <see cref="SearchOption.TopDirectoryOnly"/>.</param>
    /// <returns>Returns </returns>
    public static async Task<Shader[]> LoadFromDirectoryByExtensionAsync(this ResourceFactory factory, string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var files = Directory.GetFiles(path, searchPattern, searchOption);
        var buffer = ArrayPool<Shader>.Shared.Rent(files.Length);
        try
        {
            int shInd = 0;
            await Parallel.ForEachAsync(files, async (file, ct) =>
            {
                var type = ShaderStages.None;
                if (file.EndsWith(".vert.spv"))
                    type = ShaderStages.Vertex;
                else if (file.EndsWith(".tesc.spv"))
                    type = ShaderStages.TessellationControl;
                else if (file.EndsWith(".tese.spv"))
                    type = ShaderStages.TessellationEvaluation;
                else if (file.EndsWith(".geom.spv"))
                    type = ShaderStages.Geometry;
                else if (file.EndsWith(".frag.spv"))
                    type = ShaderStages.Fragment;
                else if (file.EndsWith(".comp.spv"))
                    type = ShaderStages.Compute;

                buffer[shInd++] = factory.CreateShader(new(type, await File.ReadAllBytesAsync(file, ct), "main"));
            });

            var final = new Shader[shInd];
            Array.ConstrainedCopy(buffer, 0, final, 0, shInd);
            return final;
        }
        finally
        {
            ArrayPool<Shader>.Shared.Return(buffer, true);
        }
    }
}
