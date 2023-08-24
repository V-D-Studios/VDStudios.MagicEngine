//using System.Diagnostics;

//namespace VDStudios.MagicEngine.Tests;

//[TestClass]
//public class GlyphDefinitionTests
//{
//    [TestMethod]
//    public void EqualityTest()
//    {
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition a = new('Z', new USize(15, 20), new UPoint(12, 12));
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition b = new('Z', new USize(15, 20), new UPoint(12, 12));
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition c = new('d', new USize(20, 30), new UPoint(1, 12));

//        Debug.Assert(a == b, "a and b evaluate to not equal despite being equal");
//        Debug.Assert(a != c, "a and c evaluate to equal despite not being equal");
//    }

//    [TestMethod]
//    public unsafe void ReadWriteToFileStream()
//    {
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition[] glyphs = new VarisizeGlyphAtlasTextRenderer.GlyphDefinition[300];
//        fixed (VarisizeGlyphAtlasTextRenderer.GlyphDefinition* start = &glyphs[0])
//        {
//            var dbytes = new Span<byte>(start, sizeof(VarisizeGlyphAtlasTextRenderer.GlyphDefinition) * glyphs.Length);
//            Random.Shared.NextBytes(dbytes);
//        }

//        using (var output = File.Open(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "out.glyphs"), FileMode.Create))
//        {
//            for (var i = 0; i < glyphs.Length; i++)
//                StructBytes.WriteBytesInto(glyphs[i], output);
//        }

//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition[] otherglyphs = new VarisizeGlyphAtlasTextRenderer.GlyphDefinition[300];
//        using (var input = File.OpenRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "out.glyphs")))
//        {
//            for (var i = 0; i < otherglyphs.Length; i++)
//                otherglyphs[i] = StructBytes.ReadBytesFrom<VarisizeGlyphAtlasTextRenderer.GlyphDefinition>(input);
//        }

//        for (int i = 0; i < glyphs.Length; i++)
//            Debug.Assert(glyphs[i] == otherglyphs[i], $"Glyph mismatch at index {i}; {glyphs[i]} != {otherglyphs[i]}");
//    }

//    private readonly unsafe int GlyphDefSize = sizeof(VarisizeGlyphAtlasTextRenderer.GlyphDefinition);

//    [TestMethod]
//    public unsafe void ReadWriteFromArray()
//    {
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition[] glyphs = new VarisizeGlyphAtlasTextRenderer.GlyphDefinition[300];
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition[] writebufferarray = new VarisizeGlyphAtlasTextRenderer.GlyphDefinition[300];
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition[] readbufferarray = new VarisizeGlyphAtlasTextRenderer.GlyphDefinition[300];

//        fixed (VarisizeGlyphAtlasTextRenderer.GlyphDefinition* gstart = &glyphs[0])
//        fixed (VarisizeGlyphAtlasTextRenderer.GlyphDefinition* wbufferstart = &writebufferarray[0])
//        {
//            var dbytes = new Span<byte>(gstart, GlyphDefSize * glyphs.Length);
//            var writebuffer = new Span<byte>(wbufferstart, GlyphDefSize * writebufferarray.Length);

//            Random.Shared.NextBytes(dbytes);

//            StructBytes.TryWriteBytesInto(glyphs, writebuffer);
//            StructBytes.TryReadBytesFrom(writebuffer, readbufferarray);
//        }

//        for (int i = 0; i < glyphs.Length; i++)
//            Debug.Assert(glyphs[i] == writebufferarray[i], $"Glyph mismatch between glyphs and writebuffer at index {i}; {glyphs[i]} != {writebufferarray[i]}");

//        for (int i = 0; i < glyphs.Length; i++)
//            Debug.Assert(writebufferarray[i] == readbufferarray[i], $"Glyph mismatch between readbuffer and writebuffer at index {i}; {writebufferarray[i]} != {readbufferarray[i]}");
//    }

//    [TestMethod]
//    public unsafe void ReadWriteToMemorySpan()
//    {
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition[] glyphs = new VarisizeGlyphAtlasTextRenderer.GlyphDefinition[300];
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition[] writebufferarray = new VarisizeGlyphAtlasTextRenderer.GlyphDefinition[300];
//        VarisizeGlyphAtlasTextRenderer.GlyphDefinition[] readbufferarray = new VarisizeGlyphAtlasTextRenderer.GlyphDefinition[300];

//        fixed (VarisizeGlyphAtlasTextRenderer.GlyphDefinition* gstart = &glyphs[0])
//        fixed (VarisizeGlyphAtlasTextRenderer.GlyphDefinition* wbufferstart = &writebufferarray[0])
//        {
//            var dbytes = new Span<byte>(gstart, GlyphDefSize * glyphs.Length);
//            var writebuffer = new Span<byte>(wbufferstart, GlyphDefSize * writebufferarray.Length);

//            Random.Shared.NextBytes(dbytes);

//            for (var i = 0; i < glyphs.Length; i++)
//                StructBytes.TryWriteBytesInto(glyphs[i], writebuffer.Slice(i * GlyphDefSize, GlyphDefSize));

//            for (var i = 0; i < readbufferarray.Length; i++)
//            {
//                if (StructBytes.TryReadBytesFrom<VarisizeGlyphAtlasTextRenderer.GlyphDefinition>(writebuffer.Slice(i * GlyphDefSize, GlyphDefSize), out var result) is false)
//                {
//                    Debug.Fail("Glyph read error");
//                    throw new InvalidOperationException("Glyph read error");
//                }

//                readbufferarray[i] = result;
//            }
//        }

//        for (int i = 0; i < glyphs.Length; i++)
//            Debug.Assert(glyphs[i] == writebufferarray[i], $"Glyph mismatch between glyphs and writebuffer at index {i}; {glyphs[i]} != {writebufferarray[i]}");

//        for (int i = 0; i < glyphs.Length; i++)
//            Debug.Assert(writebufferarray[i] == readbufferarray[i], $"Glyph mismatch between readbuffer and writebuffer at index {i}; {writebufferarray[i]} != {readbufferarray[i]}");
//    }
//}