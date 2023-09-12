using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Generators;

/// <summary>
/// Represents a type that has a default <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/>
/// </summary>
/// <typeparam name="TGraphicsVertex">The graphics vertex info generated from a <typeparamref name="TInputVertex"/></typeparam>
/// <typeparam name="TInputVertex">The input data to generate <typeparamref name="TGraphicsVertex"/></typeparam>
public interface IDefaultVertexGenerator<TInputVertex, TGraphicsVertex> : IVertexType<TGraphicsVertex>
    where TGraphicsVertex : unmanaged, IVertexType<TGraphicsVertex>
    where TInputVertex : unmanaged
{
    /// <summary>
    /// The Default <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> for this type
    /// </summary>
    public static abstract IVertexGenerator<TInputVertex, TGraphicsVertex> DefaultGenerator { get; }
}