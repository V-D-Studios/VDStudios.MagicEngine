using Serilog;
using Serilog.Events;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Logging;

/// <summary>
/// The game's logging API, used for writing log events
/// </summary>
/// <remarks>
/// The methods here are guaranteed never to throw exceptions
/// </remarks>
internal sealed class GameLogger : ILogger
{
    /// <summary>
    /// The default template
    /// </summary>
    public const string Template = "[{Timestamp:HH:mm:ss} {Level:u3}]{{{Area} -> {Facility}, \"{ObjName}\"/{ObjType} ({ObjId})}}{NewLine} > {Message:lj}{NewLine}{Exception}{NewLine}";

    public static string DefaultGameObjectIdString { get; } = default(GameObjectId).ToString()!;

    private readonly ILogger Logger;
    private readonly IGameObject Obj;
    private readonly string TypeString;

    internal GameLogger(ILogger logger, IGameObject obj)
    {
        Logger = logger;
        Obj = obj;
        TypeString = IGameObject.CreateGameObjectName(obj);
    }

    public static void Write(ILogger Logger, LogEvent logEvent, string Facility, string Area, Type OwningType, string typeString, string? ObjName, string id)
    {
        if (Logger.BindProperty("Facility", Facility, false, out var prop))
            logEvent.AddPropertyIfAbsent(prop);
        else if (Logger.BindProperty("Facility", "Unknown", false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        if (Logger.BindProperty("Area", Area, false, out prop))
            logEvent.AddPropertyIfAbsent(prop);
        else if (Logger.BindProperty("Area", "Unknown", false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        if (Logger.BindProperty("ObjTypeFull", OwningType.AssemblyQualifiedName, false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        if (Logger.BindProperty("ObjType", typeString, false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        if (Logger.BindProperty("ObjName", ObjName ?? "Unnamed", false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        if (Logger.BindProperty("ObjId", id, false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        Logger.Write(logEvent);
    }

    /// <summary>
    /// Writes an event to the log
    /// </summary>
    /// <param name="logEvent">The event to write</param>
    /// <exception cref="NotImplementedException"></exception>
    public void Write(LogEvent logEvent)
        => Write(Logger, logEvent, Obj.Facility, Obj.Area, Obj.GetType(), TypeString, Obj.Name, Obj.IdString);
}
