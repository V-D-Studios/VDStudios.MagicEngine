using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public const string Template = "{{{Area} -> {Facility}, {Author}}}[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    private readonly ILogger Logger;
    private readonly string Facility;
    private readonly string Area;
    private readonly Type OwningType;

    internal GameLogger(ILogger logger, string area, string facility, Type owningType)
    {
        Logger = logger;
        Facility = facility;
        Area = area;
        OwningType = owningType;
    }

    public static void Write(ILogger Logger, LogEvent logEvent, string Facility, string Area, Type OwningType)
    {
        if (Logger.BindProperty("Facility", Facility, false, out var prop))
            logEvent.AddPropertyIfAbsent(prop);
        else if (Logger.BindProperty("Facility", "Unknown", false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        if (Logger.BindProperty("Area", Area, false, out prop))
            logEvent.AddPropertyIfAbsent(prop);
        else if (Logger.BindProperty("Area", "Unknown", false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        if (Logger.BindProperty("Author", OwningType.Name, false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        if (Logger.BindProperty("AuthorFull", OwningType.AssemblyQualifiedName, false, out prop))
            logEvent.AddPropertyIfAbsent(prop);

        Logger.Write(logEvent);
    }

    /// <summary>
    /// Writes an event to the log
    /// </summary>
    /// <param name="logEvent">The event to write</param>
    /// <exception cref="NotImplementedException"></exception>
    public void Write(LogEvent logEvent)
        => Write(Logger, logEvent, Facility, Area, OwningType);
}
