using System.Runtime.InteropServices;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Represents a Circumference's radius
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct Radius
{
    /// <summary>
    /// The radius of the circumference
    /// </summary>
    public readonly float Value;

    /// <summary>
    /// Radius times two (r*2)
    /// </summary>
    public float Diameter => Value * 2;

    /// <summary>
    /// Creates a new instance of <see cref="Radius"/> using a diameter value
    /// </summary>
    /// <returns></returns>
    public static Radius ByDiameter(float value) => new(value / 2);

    /// <summary>
    /// Creates a new instance of <see cref="Radius"/>
    /// </summary>
    private Radius(float value) => Value = value;

    /// <summary>
    /// Implicitly casts an instance of <see cref="float"/> to <see cref="Radius"/>
    /// </summary>
    public static implicit operator float(Radius r) => r.Value;

    /// <summary>
    /// Implicitly casts an instance of <see cref="Radius"/> to <see cref="float"/>
    /// </summary>
    public static implicit operator Radius(float r) => new(r);
}
