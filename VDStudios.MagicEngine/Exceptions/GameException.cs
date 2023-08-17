using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// Represents an exception thrown due to an error in a <see cref="Game"/>
/// </summary>
[Serializable]
public class GameException : Exception
{
	/// <inheritdoc/>
	public GameException() { }

    /// <inheritdoc/>
    public GameException(string message) : base(message) { }

    /// <inheritdoc/>
    public GameException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc/>
    protected GameException(
	  System.Runtime.Serialization.SerializationInfo info,
	  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
