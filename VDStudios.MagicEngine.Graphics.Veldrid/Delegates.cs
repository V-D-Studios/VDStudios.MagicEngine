using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Represents a method that can be used to produce a graphics resource
/// </summary>
/// <param name="context">The resource context</param>
/// <typeparam name="T">The type of resource</typeparam>
/// <returns>The produced resource</returns>
public delegate T GraphicsResourceFactory<T>(IVeldridGraphicsContextResources context)
    where T : class;

/// <summary>
/// Represents a method that can be used to produce a graphics resource
/// </summary>
/// <param name="context">The resource context</param>
/// <typeparam name="TResource">The type of resource</typeparam>
/// <typeparam name="TDependency">The type of the dependency the resource has</typeparam>
/// <returns>The produced resource</returns>
public delegate TResource GraphicsResourceFactory<TDependency, TResource>(IVeldridGraphicsContextResources context, TDependency dependency)
    where TResource : class
    where TDependency : class;