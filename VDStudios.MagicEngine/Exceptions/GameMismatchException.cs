using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when an attempt is made to couple two or more objects that have unexpectedly mismatching games. I.e. Attaching a node to a scene that belongs to a different <see cref="Game"/> instance than the node
/// </summary>
[Serializable]
public class GameMismatchException : Exception
{
    /// <summary>
    /// Provides details over the differences across all the objects
    /// </summary>
    public string MismatchDetails { get; }

    private static string BuildDetailedMessage(IGameObject[] objects, string[] objectExpressions)
    {
        int latest = 0;
        Debug.Assert(objects.Length == objectExpressions.Length, "objects and objectExpressions have different lengths");
        using (SharedObjectPools.StringBuilderPool.Rent(out var sb))
        using (SharedObjectPools.StringBuilderPool.Rent(out var games))
        {
            Dictionary<Game, int> dict = new();

            for (int i = 0; i < objects.Length; i++)
            {
                sb.Append($"{objectExpressions[i]} {objects[i].GetType()}");
                if (objects[i].Name is string name)
                    sb.Append($" ({name})");
                else
                    sb.Append(" belonging to detected game #");

                sb.AppendLine(GetLocalGameId(objects[i].Game).ToString());
            }

            games.Append("Games detected in this mismatch:\n\n");
            foreach (var (k, v) in dict)
                games.Append($"- {k.GetType()}: #{v}");

            return $"{games}\n\n------------\n\n{sb}";

            int GetLocalGameId(Game game)
            {
                if (dict.TryGetValue(game, out int value) is false)
                    dict[game] = value = latest++;
                return value;
            }
        }
    }

    private static string BuildMessage(IGameObject[] objects, string[] objectExpressions)
    {
        Debug.Assert(objects.Length == objectExpressions.Length, "objects and objectExpressions have different lengths");
        using (SharedObjectPools.StringBuilderPool.Rent(out var sb))
        {
            sb.Append("The following GameObjects have a mismatching game: ");
            for (int i = 0; i < objects.Length; i++)
            {
                sb.Append($"{objectExpressions[i]} {objects[i].GetType()}");
                if (objects[i].Name is string name)
                    sb.Append($" ({name})");
                else
                    sb.Append("; ");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="GameMismatchException"/>
    /// </summary>
    public GameMismatchException(IGameObject[] objects, string[] objectExpressions)
        : base(BuildMessage(objects, objectExpressions)) 
    {
        Debug.Assert(objects.Length == objectExpressions.Length, "objects and objectExpressions have different lengths");
        MismatchDetails = BuildDetailedMessage(objects, objectExpressions);
    }

    /// <summary>
    /// Creates a new instance of <see cref="GameMismatchException"/>
    /// </summary>
    public static void ThrowIfMismatch(
        IGameObject a, 
        IGameObject b, 
        [CallerArgumentExpression(nameof(a))] string? aexpression = null, 
        [CallerArgumentExpression(nameof(b))] string? bexpression = null
    )
    {
        if (a.Game != b.Game)
            throw new GameMismatchException(
                  new IGameObject[]
                  {
                      a,
                      b
                  },
                  new string[]
                  {
                      aexpression ?? "",
                      bexpression ?? ""
                  }
            );
    }

    /// <summary>
    /// Creates a new instance of <see cref="GameMismatchException"/>
    /// </summary>
    public static void ThrowIfMismatch(
        IGameObject a,
        IGameObject b,
        IGameObject c,
        [CallerArgumentExpression(nameof(a))] string? aexpression = null,
        [CallerArgumentExpression(nameof(b))] string? bexpression = null,
        [CallerArgumentExpression(nameof(c))] string? cexpression = null
    )
    {
        if (a.Game != b.Game || b.Game != c.Game || c.Game != a.Game) 
            throw new GameMismatchException(
              new IGameObject[]
              {
                  a,
                  b,
                  c
              },
              new string[]
              {
                  aexpression ?? "",
                  bexpression ?? "",
                  cexpression ?? ""
              }
        );
    }

    /// <summary>
    /// Creates a new instance of <see cref="GameMismatchException"/>
    /// </summary>
    public static void ThrowIfMismatch(
        IGameObject a,
        IGameObject b,
        IGameObject c,
        IGameObject d,
        [CallerArgumentExpression(nameof(a))] string? aexpression = null,
        [CallerArgumentExpression(nameof(b))] string? bexpression = null,
        [CallerArgumentExpression(nameof(c))] string? cexpression = null,
        [CallerArgumentExpression(nameof(d))] string? dexpression = null
    )
    {
        if (a.Game != b.Game || a.Game != c.Game || a.Game != d.Game ||
            b.Game != c.Game || b.Game != d.Game ||
            c.Game != d.Game) 
            throw new GameMismatchException(
              new IGameObject[]
              {
                  a,
                  b,
                  c,
                  d
              },
              new string[]
              {
                  aexpression ?? "",
                  bexpression ?? "",
                  cexpression ?? "",
                  dexpression ?? ""
              }
        );
    }

    /// <inheritdoc/>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    protected GameMismatchException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
