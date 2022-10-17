using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a schedule that can have work queued up to it to be executed at a later, set time
/// </summary>
public sealed class DeferredExecutionSchedule
{
    private TimeSpan PrevStamp;
    private readonly Stopwatch Watch;
    private readonly LinkedList<DeferredCallInfo> OneTimeSchedule;
    private readonly LinkedList<TimedActionInfo> TimedActionSchedule;
    private ulong CurrentFrame = 1;

    /// <summary>
    /// Creates a new <see cref="DeferredExecutionSchedule"/>, and grants access to its Update method
    /// </summary>
    /// <remarks>
    /// <paramref name="updater"/> must be called repeatedly (usually, every frame) in order for this class to function properly, and it must be owned privately, hence the existence of this static method
    /// </remarks>
    /// <param name="updater">The <see cref="Action"/> that represents the update method for the <see cref="DeferredExecutionSchedule"/> returned by this method</param>
    public DeferredExecutionSchedule(out Action updater)
    {
        OneTimeSchedule = new();
        TimedActionSchedule = new();
        Watch = new();
        Watch.Start();
        updater = Update;
    }

    private void Update()
    {
        lock (Watch) // Ensure only one Update is called at a time
        {
            var stamp = Watch.Elapsed;
            var delta = stamp - PrevStamp;
            PrevStamp = stamp;

            var otsNode = OneTimeSchedule.First;
            var count = OneTimeSchedule.Count;
            while (otsNode is not null)
            {
                ref var info = ref otsNode.ValueRef;
                if ((CurrentFrame % info.Frames) == 0 || stamp > info.Time)
                {
                    var prevNode = otsNode;
                    otsNode = prevNode.Next;
                    lock (OneTimeSchedule)
                        OneTimeSchedule.Remove(prevNode);
                    info.Action(this, stamp - info.Registered);
                }
                else
                    otsNode = otsNode.Next;
            }

            var tasNode = TimedActionSchedule.First;
            count = TimedActionSchedule.Count;
            while (tasNode is not null)
            {
                ref var info = ref tasNode.ValueRef;
                info.Action(this, delta);
                if ((CurrentFrame % info.Frames) == 0 || stamp > info.Time)
                {
                    var prevNode = tasNode;
                    tasNode = prevNode.Next;
                    lock (TimedActionSchedule)
                        TimedActionSchedule.Remove(prevNode);
                }
                else
                    tasNode = tasNode.Next;
            }

            CurrentFrame++;
        }
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run for <paramref name="time"/> amount of time
    /// </summary>
    /// <param name="action">The action to call</param>
    /// <param name="time">The amount of time this call will be repeated (per-frame) for</param>
    public void CallFor(UpdateEvent<DeferredExecutionSchedule> action, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (time <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(time), "The amount of time to defer action for must be larger than 0");

        lock (TimedActionSchedule)
            TimedActionSchedule.AddLast(new TimedActionInfo(
                Action: action,
                Frames: AssortedExtensions.PrimeNumberNearestToUInt16MaxValue,
                Time: time + Watch.Elapsed
            ));
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run for <paramref name="frames"/> frames
    /// </summary>
    /// <param name="action">The action to call</param>
    /// <param name="frames">The amount of frames this call will be repeated for</param>
    public void CallFor(UpdateEvent<DeferredExecutionSchedule> action, ushort frames)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (frames <= 0)
            throw new ArgumentOutOfRangeException(nameof(frames), "The amount of frames to defer action for must be larger than 0");

        lock (TimedActionSchedule)
            TimedActionSchedule.AddLast(new TimedActionInfo(
                Action: action,
                Frames: (uint)(CurrentFrame % frames + frames + 1),
                Time: TimeSpan.MaxValue
            ));
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run after <paramref name="time"/> amount of time
    /// </summary>
    /// <remarks>
    /// A frame in this context is defined as an update frame fro this <see cref="DeferredExecutionSchedule"/>
    /// </remarks>
    /// <param name="action">The action to call</param>
    /// <param name="time">The amount of time to wait before calling <paramref name="action"/></param>
    public void DeferCall(UpdateEvent<DeferredExecutionSchedule> action, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (time <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(time), "The amount of time to defer action for must be larger than 0");

        lock (OneTimeSchedule)
            OneTimeSchedule.AddLast(new DeferredCallInfo(
                Action: action,
                Frames: AssortedExtensions.PrimeNumberNearestToUInt16MaxValue,
                Time: time + Watch.Elapsed,
                Registered: Watch.Elapsed
            ));
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run after <paramref name="frames"/> amount of frames
    /// </summary>
    /// <remarks>
    /// A frame in this context is defined as an update frame fro this <see cref="DeferredExecutionSchedule"/>
    /// </remarks>
    /// <param name="action">The action to call</param>
    /// <param name="frames">The amount of frames to wait before calling <paramref name="action"/></param>
    public void DeferCall(UpdateEvent<DeferredExecutionSchedule> action, ushort frames)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (frames <= 0)
            throw new ArgumentOutOfRangeException(nameof(frames), "The amount of frames to defer action for must be larger than 0");

        lock (OneTimeSchedule)
            OneTimeSchedule.AddLast(new DeferredCallInfo(
                Action: action,
                Frames: (uint)(CurrentFrame % frames + frames + 1),
                Time: TimeSpan.MaxValue,
                Registered: Watch.Elapsed
            ));
    }

    // Schedule in the Game thread
    //   In {TimeSpan} time
    //   In n updates
    //   Recurrent?
    //   Returning?

    #region Helpers

    private record struct TimedActionInfo(UpdateEvent<DeferredExecutionSchedule> Action, TimeSpan Time, uint Frames);

    private record struct DeferredCallInfo(UpdateEvent<DeferredExecutionSchedule> Action, TimeSpan Time, uint Frames, TimeSpan Registered);

    #endregion
}
