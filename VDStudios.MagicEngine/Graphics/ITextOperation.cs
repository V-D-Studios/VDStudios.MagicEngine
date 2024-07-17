using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// An operation that renders text
/// </summary>
/// <typeparam name="TGraphicsContext"></typeparam>
public interface ITextOperation<TGraphicsContext> : IDrawOperation<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// The Text that is currently set to be rendered by this <see cref="ITextOperation{TGraphicsContext}"/>
    /// </summary >
    string? Text { get; set; }
}

