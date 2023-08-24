using System;
using System.Diagnostics;
using System.Threading;

namespace VDStudios.MagicEngine.Tests;

[TestClass]
public class StopwatchTest
{
    [TestMethod]
    public void Test()
    {
        var sw = new Stopwatch();
        var interval = TimeSpan.FromSeconds(10);

        Console.WriteLine("hola");

        sw.Start();

        Thread.Sleep(10_000);

        Console.WriteLine(sw.Elapsed.Seconds % interval.Seconds);

        sw.Restart();

        Thread.Sleep(30_000);

        Console.WriteLine(sw.Elapsed.Seconds % interval.Seconds);

        Console.WriteLine("hola");
    }
}