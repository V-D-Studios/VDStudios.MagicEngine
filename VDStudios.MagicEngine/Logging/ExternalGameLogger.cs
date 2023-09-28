using Serilog;
using Serilog.Events;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Logging;

/// <summary>
/// Much like <see cref="GameLogger"/>, but instead of holding a reference to a specific <see cref="IGameObject"/>, it simply maintains static data about the non-game object it's logging for
/// </summary>
internal sealed class ExternalGameLogger : ILogger
{
    /// <summary>
    /// The default template
    /// </summary>
    public const string Template = "[{Timestamp:HH:mm:ss} {Level:u3}]{{{Area} -> {Facility}, \"{ObjName}\"/{ObjType}}}{NewLine} > {Message:lj}{NewLine}{Exception}{NewLine}";

    public static string DefaultGameObjectIdString { get; } = default(GameObjectId).ToString()!;

    private readonly ILogger Logger;
    private readonly string TypeString;

    private readonly string Facility;
    private readonly string Area;
    private readonly string? Name;
    private readonly Type OwningType;

    internal ExternalGameLogger(ILogger logger, string facility, string area, Type owningtype, string? objName)
    {
        Logger = logger;
        Facility = facility;
        Area = area;
        OwningType = owningtype;
        Name = objName;
        TypeString = Helper.BuildTypeNameAsCSharpTypeExpression(owningtype);
    }

    public static void Write(ILogger Logger, LogEvent logEvent, string Facility, string Area, Type OwningType, string typeString, string? ObjName)
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

        Logger.Write(logEvent);
    }

    /// <summary>
    /// Writes an event to the log
    /// </summary>
    /// <param name="logEvent">The event to write</param>
    /// <exception cref="NotImplementedException"></exception>
    public void Write(LogEvent logEvent)
        => Write(Logger, logEvent, Facility, Area, GetType(), TypeString, Name);
}
