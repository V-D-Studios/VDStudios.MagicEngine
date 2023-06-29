using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Information about a Draw Operation's current Draw Target
/// </summary>
/// <param name="TargetIndex">Represents the index of <paramref name="Target"/></param>
/// <param name="TargetCount">The amount of targets this operation will be rendered to</param>
/// <param name="Target">The framebuffer being targeted</param>
/// <param name="Parameters">The parameters that are to be used in the operation</param>
public readonly record struct FramebufferTargetInfo(int TargetIndex, int TargetCount, Framebuffer Target, DrawParameters Parameters);
