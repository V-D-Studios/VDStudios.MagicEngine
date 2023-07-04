using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using VDStudios.MagicEngine.Benchmarks.Serialization;

namespace VDStudios.MagicEngine.Benchmarks;

internal static class Program
{
    private static void Main(string[] args)
    {
        var benchmarkTypes 
            = Assembly.GetCallingAssembly().GetTypes().Where(x => x.GetMethods().Any(x => x.GetCustomAttribute<BenchmarkAttribute>() is not null)).ToImmutableArray();

        int index = 0;

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("Use the up/down or pageUp/pageDown keys to navigate. Press enter to select a benchmark\n");
        Console.ForegroundColor = ConsoleColor.Gray;

        for (int i = 0; i < benchmarkTypes.Length; i++)
            Console.WriteLine($" > {benchmarkTypes[i].Name}");

        while (true)
        {
            Thread.Sleep(200);

            Console.SetCursorPosition(0, index + 2);
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" > {benchmarkTypes[index].Name}".PadRight(Console.BufferWidth));

            var pressed = Console.ReadKey(true);
            if (pressed.Key is ConsoleKey.UpArrow or ConsoleKey.PageUp)
                index = (index - 1) % benchmarkTypes.Length;
            else if (pressed.Key is ConsoleKey.DownArrow or ConsoleKey.PageDown)
                index = (index + 1) % benchmarkTypes.Length;
            else if (pressed.Key is ConsoleKey.Enter)
            {
                Console.ResetColor();
                Console.Clear();

                Console.WriteLine("Running verification tests...");
                var t = benchmarkTypes[index];
                var x = Activator.CreateInstance(t);
                foreach (var method in t.GetMethods().Where(x => x.GetCustomAttribute<BenchmarkAttribute>() is not null))
                    method.Invoke(x, null);
                Console.WriteLine("Verification complete, summoning benchmark");
                Thread.Sleep(300);
                Console.Clear();

                BenchmarkRunner.Run(t);
                break;
            }
        }

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        Console.ResetColor();
    }
}
