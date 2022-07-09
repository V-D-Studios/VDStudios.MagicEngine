using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Provides parameters and other useful data to be used when drawing in a <see cref="DrawOperation"/>
/// </summary>
/// <param name="Transform">The transformation matrix in these parameters</param>
public readonly record struct DrawParameters(Matrix4x4 Transform);
