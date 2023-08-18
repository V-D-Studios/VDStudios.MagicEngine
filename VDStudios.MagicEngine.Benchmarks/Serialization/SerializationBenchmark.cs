using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using MessagePack;

namespace VDStudios.MagicEngine.Benchmarks.Serialization;

[MessagePackObject]
public class Cat
{
    [Key(0)]
    public int Age { get; set; }

    [Key(1)]
    public int PetId { get; set; }
}

[MessagePackObject]
public class Person
{
    [Key(0)]
    public int Age { get; set; }

    [Key(1)]
    public DateTime Birthday { get; set; }

    [Key(2)]
    public decimal Wage { get; set; }

    [Key(3)]
    public Cat Pet { get; set; }
}

[MessagePackObject]
public struct PersonBuffer
{
    [Key(0)]
    public int Age { get; set; }

    [Key(2)]
    public DateTime Birthday { get; set; }

    [Key(3)]
    public decimal Wage { get; set; }

    [Key(4)]
    public int PetAge { get; set; }

    [Key(5)]
    public int PetId { get; set; }
}

[MessagePackObject]
public struct NumberSummary
{
    public NumberSummary(int alpha, float beta, double gamma, decimal delta, long epsilon, short theta, char iota, byte kappa)
    {
        Alpha = alpha;
        Beta = beta;
        Gamma = gamma;
        Delta = delta;
        Epsilon = epsilon;
        Theta = theta;
        Iota = iota;
        Kappa = kappa;
    }

    [Key(0)]
    public int Alpha { get; set; }

    [Key(1)]
    public float Beta { get; set; }

    [Key(2)]
    public double Gamma { get; set; }

    [Key(3)]
    public decimal Delta { get; set; }

    [Key(4)]
    public long Epsilon { get; set; }

    [Key(5)]
    public short Theta { get; set; }

    [Key(6)]
    public char Iota { get; set; }

    [Key(7)]
    public byte Kappa { get; set; }
}

[MemoryDiagnoser]
public class SerializationBenchmark
{
    private readonly string[] Names = new string[] { "Michigan", "Duke", "Luke", "Valery", "Lucia", "Diego", "Samuel", "Antonio", "Daniel", "Agatha", "Jesus", "Maria", "Minina" };
    private string RandomName()
        => Names[Random.Shared.Next(0, Names.Length - 1)];
    private Cat GenRandomPet()
        => new()
        {
            Age = Random.Shared.Next(0, 10)
        };

    private DateTime RandomDate()
        => new(Random.Shared.Next(1990, 2020), Random.Shared.Next(1, 12), Random.Shared.Next(1, 28));

    private Person GenRandomPerson()
        => new()
        {
            Age = Random.Shared.Next(4, 55),
            Birthday = RandomDate(),
            Wage = (decimal)(Random.Shared.Next(15_000, 100_000) * Random.Shared.NextDouble() + .1),
            Pet = GenRandomPet()
        };

    public readonly Person[] Persons = new Person[300];
    public readonly NumberSummary[] Numbers = new NumberSummary[300];
    public readonly byte[] NumberBinaryBuffer = new byte[Unsafe.SizeOf<NumberSummary>() * 300];

    public unsafe SerializationBenchmark()
    {
        for (int i = 0; i < Persons.Length; i++)
            Persons[i] = GenRandomPerson();

        var handle = GCHandle.Alloc(Numbers, GCHandleType.Pinned);
        try
        {
            NumberSummary* dptr = (NumberSummary*)Unsafe.AsPointer(ref Numbers[0]);
            Span<byte> dbytes = new(dptr, sizeof(NumberSummary) * Numbers.Length);
            Random.Shared.NextBytes(dbytes);
        }
        finally
        {
            handle.Free();
        }

        for (int i = 0; i < Numbers.Length; i++)
        {
            ref var ns = ref Numbers[i];
            ns.Delta = (decimal)(Random.Shared.NextDouble() * Random.Shared.NextDouble());
            ns.Beta = (float)(Random.Shared.NextDouble() * Random.Shared.NextDouble());
            ns.Gamma = (Random.Shared.NextDouble() * Random.Shared.NextDouble());
        }
    }

    [Benchmark]
    public void SystemTextJson_PersonClass()
    {
        for (int i = 0; i < Persons.Length; i++)
        {
            var json = JsonSerializer.Serialize(Persons[i]);
            Persons[i] = JsonSerializer.Deserialize<Person>(json)!;
        }
    }

    [Benchmark]
    public void MessagePack_PersonClass()
    {
        for (int i = 0; i < Persons.Length; i++)
        {
            var json = MessagePackSerializer.Serialize(Persons[i]);
            Persons[i] = MessagePackSerializer.Deserialize<Person>(json)!;
        }
    }

    [Benchmark]
    public void RawBytes_BufferedPersonClass()
    {
        Span<byte> bytes = stackalloc byte[Unsafe.SizeOf<PersonBuffer>()];
        for (int i = 0; i < Persons.Length; i++)
        {
            var buffer = new PersonBuffer()
            {
                Age = Persons[i].Age,
                Birthday = Persons[i].Birthday,
                Wage = Persons[i].Wage,
                PetAge = Persons[i].Pet.Age,
                PetId = Persons[i].Pet.PetId
            };

            StructBytes.TryWriteBytesInto(buffer, bytes);
            if (StructBytes.TryReadBytesFrom<PersonBuffer>(bytes, out var outbuf) is false)
                throw new InvalidOperationException("Could not read back person info");

            Persons[i] = new Person()
            {
                Age = outbuf.Age,
                Birthday = outbuf.Birthday,
                Wage = outbuf.Wage,
                Pet = new()
                {
                    Age = outbuf.PetAge,
                    PetId = outbuf.PetId
                }
            };
        }
    }

    [Benchmark]
    public void SystemTextJson_NumberSummary()
    {
        for (int i = 0; i < Numbers.Length; i++)
        {
            var json = JsonSerializer.Serialize(Numbers[i]);
            Numbers[i] = JsonSerializer.Deserialize<NumberSummary>(json)!;
        }
    }

    [Benchmark]
    public void MessagePack_NumberSummary()
    {
        for (int i = 0; i < Numbers.Length; i++)
        {
            var json = MessagePackSerializer.Serialize(Numbers[i]);
            Numbers[i] = MessagePackSerializer.Deserialize<NumberSummary>(json)!;
        }
    }

    [Benchmark]
    public void RawBytes_NumberSummary()
    {
        StructBytes.TryWriteBytesInto(Numbers, NumberBinaryBuffer);
        StructBytes.TryReadBytesFrom(NumberBinaryBuffer, Numbers);
    }
}
