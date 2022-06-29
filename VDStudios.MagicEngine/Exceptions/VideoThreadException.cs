using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when the <see cref="Game"/>'s video thread throws an exception
/// </summary>
/// <remarks>
/// This class cannot be inherited or instanced by user code
/// </remarks>
[Serializable]
public sealed class VideoThreadException : Exception
{
    internal VideoThreadException(Exception exception) : base("The Game's VideoThread has thrown an exception and can no longer perform any work", exception) { }
}
