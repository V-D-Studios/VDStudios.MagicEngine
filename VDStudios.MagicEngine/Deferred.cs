using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a schedule that can have work queued up to it to be executed at a later, set time
/// </summary>
public sealed class DeferredExecutionSchedule
{
    private readonly Stopwatch Watch;
    private readonly LinkedList<DeferredCallInfo> OneTimeSchedule;
    private readonly List<Action> ActionList;
    private ulong CurrentFrame;

    private DeferredExecutionSchedule()
    {
        OneTimeSchedule = new();
        Watch = new();
        Watch.Start();
        ActionList = new();
        Updater = Update;
    }

    /// <summary>
    /// Creates a new <see cref="DeferredExecutionSchedule"/>, and grants access to its Update method
    /// </summary>
    /// <remarks>
    /// <paramref name="updater"/> must be called repeatedly (usually, every frame) in order for this class to function properly, and it must be owned privately, hence the existence of this static method
    /// </remarks>
    /// <param name="updater">The <see cref="Action"/> that represents the update method for the <see cref="DeferredExecutionSchedule"/> returned by this method</param>
    /// <returns>A new <see cref="DeferredExecutionSchedule"/></returns>
    public static DeferredExecutionSchedule New(out Action updater)
    {
        var des = new DeferredExecutionSchedule();
        updater = des.Updater;
        return des;
    }

    private readonly Action Updater;
    private void Update()
    {
        lock (Watch) // Ensure only one Update is called at a time
        {
            var stamp = Watch.Elapsed;
            var node = OneTimeSchedule.First;
            var count = OneTimeSchedule.Count;
            while (node is not null)
            {
                ref var info = ref node.ValueRef;
                if ((CurrentFrame % info.Frames) == 0 || stamp > info.Time)
                {
                    ActionList.Add(info.Action);
                    var prevNode = node;
                    node = prevNode.Next;
                    lock (OneTimeSchedule)
                        OneTimeSchedule.Remove(prevNode);
                }
                else
                    node = node.Next;
            }
            CurrentFrame++;
        }
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run after <paramref name="time"/> amount of time
    /// </summary>
    /// <remarks>
    /// A frame in this context is defined as an update frame fro this <see cref="DeferredExecutionSchedule"/>
    /// </remarks>
    /// <param name="action">The action to call</param>
    /// <param name="time">The amount of time to wait before calling <paramref name="action"/></param>
    public void Schedule(Action action, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (time <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(time), "The amount of time to defer action for must be larger than 0");

        lock (OneTimeSchedule)
            OneTimeSchedule.AddLast(new DeferredCallInfo()
            {
                Action = action,
                Frames = ushort.MaxValue,
                Time = time + Watch.Elapsed
            });
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run after <paramref name="frames"/> amount of frames
    /// </summary>
    /// <remarks>
    /// A frame in this context is defined as an update frame fro this <see cref="DeferredExecutionSchedule"/>
    /// </remarks>
    /// <param name="action">The action to call</param>
    /// <param name="frames">The amount of frames to wait before calling <paramref name="action"/></param>
    public void Schedule(Action action, ushort frames)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (frames <= 0)
            throw new ArgumentOutOfRangeException(nameof(frames), "The amount of frames to defer action for must be larger than 0");

        lock (OneTimeSchedule)
            OneTimeSchedule.AddLast(new DeferredCallInfo()
            {
                Action = action,
                Frames = (uint)(CurrentFrame % frames + frames + 1),
                Time = TimeSpan.MaxValue
            });
    }

    // Schedule in the Game thread
    //   In {TimeSpan} time
    //   In n updates
    //   Recurrent?
    //   Returning?

    #region Helpers

    private struct DeferredCallInfo
    {
        public required Action Action { get; init; }
        public required TimeSpan Time { get; init; }
        public required uint Frames { get; init; }
    }

    #endregion
}
