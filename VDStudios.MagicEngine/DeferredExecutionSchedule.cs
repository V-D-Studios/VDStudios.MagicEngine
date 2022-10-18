using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private TimeSpan Stamp;
    private TimeSpan PrevStamp;
    private TimeSpan Delta;
    private readonly SemaphoreSlim Sem = new(1, 1);
    private readonly Stopwatch Watch;
    private readonly LinkedList<DeferredCallInfo> OneTimeSchedule;
    private readonly LinkedList<TimedActionInfo> TimedActionSchedule;
    private ulong CurrentFrame = 1;

#if VALIDATE_USAGE
    private readonly HashSet<WeakReference<RecurrentCallHandle>> RecurrentCallsSchedule;
#else
    private readonly HashSet<RecurrentCallHandle> RecurrentCallsSchedule;
#endif

    /// <summary>
    /// Creates a new <see cref="DeferredExecutionSchedule"/>, and grants access to its Update method
    /// </summary>
    /// <remarks>
    /// <paramref name="updater"/> must be called repeatedly (usually, every frame) in order for this class to function properly, and it must be owned privately, hence the existence of this static method
    /// </remarks>
    /// <param name="updater">The <see cref="Func{TReturn}"/> that represents the update method for the <see cref="DeferredExecutionSchedule"/> returned by this method</param>
    public DeferredExecutionSchedule(out Func<ValueTask> updater)
    {
        OneTimeSchedule = new();
        TimedActionSchedule = new();
        RecurrentCallsSchedule = new();
        Watch = new();
        Watch.Start();
        updater = Update;
    }

    private void UpdateOneTimeSchedule()
    {
        var otsNode = OneTimeSchedule.First;
        var count = OneTimeSchedule.Count;
        while (otsNode is not null)
        {
            ref var info = ref otsNode.ValueRef;
            if ((CurrentFrame % info.Frames) == 0 || Stamp > info.Time)
            {
                var prevNode = otsNode;
                otsNode = prevNode.Next;
                lock (OneTimeSchedule)
                    OneTimeSchedule.Remove(prevNode);
                info.Action(this, Stamp - info.Registered);
            }
            else
                otsNode = otsNode.Next;
        }
    }

    private void UpdateTimedActionsSchedule()
    {
        var tasNode = TimedActionSchedule.First;
        var count = TimedActionSchedule.Count;
        while (tasNode is not null)
        {
            ref var info = ref tasNode.ValueRef;
            info.Action(this, Delta);
            if ((CurrentFrame % info.Frames) == 0 || Stamp > info.Time)
            {
                var prevNode = tasNode;
                tasNode = prevNode.Next;
                lock (TimedActionSchedule)
                    TimedActionSchedule.Remove(prevNode);
            }
            else
                tasNode = tasNode.Next;
        }
    }

    private void UpdateRecurrentCallsSchedule()
    {
        lock (RecurrentCallsSchedule)
        {
#if VALIDATE_USAGE
            var arr = ArrayPool<WeakReference<RecurrentCallHandle>>.Shared.Rent(RecurrentCallsSchedule.Count);
            try
            {
                RecurrentCallsSchedule.CopyTo(arr);
                foreach (var wrinfo in arr)
                    if (wrinfo.TryGetTarget(out var info))
                    {
                        if ((CurrentFrame % info.Frames) == 0 || Stamp > info.Time)
                            info.ActionRef(this, Delta);
                    }
                    else
                    {
                        RecurrentCallsSchedule.Remove(wrinfo);
                        throw new ObjectDisposedException(nameof(RecurrentCallHandle), "A RecurrentCallHandle was reclaimed by the GC unexpectedly. This schedule only maintains weak references, and client code must maintain a reference to the object until it's disposed.");
                    }
            }
            finally
            {
                ArrayPool<WeakReference<RecurrentCallHandle>>.Shared.Return(arr);
            }
#else
            foreach (var info in RecurrentCallsSchedule)
                if ((CurrentFrame % info.Frames) == 0 || Stamp > info.Time)
                    info.ActionRef(this, Delta);
#endif
        }
    }

    Task? onet;
    Task? tima;
    Task? recc;
    private async ValueTask Update()
    {
        Stamp = Watch.Elapsed;
        Delta = Stamp - PrevStamp;
        PrevStamp = Stamp;

        Sem.Wait();
        try // Ensure only one Update is called at a time
        {
            if (OneTimeSchedule.Count > 40)
                onet = Task.Run(UpdateOneTimeSchedule);
            else
                UpdateOneTimeSchedule();

            if (TimedActionSchedule.Count > 40)
                tima = Task.Run(UpdateTimedActionsSchedule);
            else
                UpdateTimedActionsSchedule();

            if (RecurrentCallsSchedule.Count > 40)
                recc = Task.Run(UpdateRecurrentCallsSchedule);
            else
                UpdateRecurrentCallsSchedule();

            if (onet is not null)
                await onet;
            if (tima is not null)
                await tima;
            if (recc is not null)
                await recc;

            CurrentFrame++;
        }
        finally
        {
            onet = null;
            tima = null;
            recc = null;
            Sem.Release();
        }
    }

#if VALIDATE_USAGE
    /// <summary>
    /// Schedules <paramref name="action"/> to run indefinitely every time <paramref name="time"/> elapses
    /// </summary>
    /// <param name="action">The action to call</param>
    /// <param name="time">The amount of time to wait each time before calling <paramref name="action"/></param>
    /// <returns>
    /// An <see cref="IDisposable"/> object that, when disposed, removes the recurrent call from the schedule. If this object is claimed by the GC before being disposed, the <see cref="DeferredExecutionSchedule"/> that produced this object will eventually throw an Exception, but finalization is non-deterministic, can be run at any time and is not guaranteed to run at all.
    /// </returns>
#else
    /// <summary>
    /// Schedules <paramref name="action"/> to run indefinitely every time <paramref name="time"/> elapses
    /// </summary>
    /// <param name="action">The action to call</param>
    /// <param name="time">The amount of time to wait each time before calling <paramref name="action"/></param>
    /// <returns>
    /// An <see cref="IDisposable"/> object that, when disposed, removes the recurrent call from the schedule. If this object is claimed by the GC before being disposed, it will also result in the delegate being de-scheduled, but finalization is non-deterministic, can be run at any time and is not guaranteed to run at all.
    /// </returns>
#endif
    public IDisposable ScheduleRecurrentCall(UpdateEvent<DeferredExecutionSchedule> action, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (time <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(time), "The amount of time to defer action for must be larger than 0");

        var rch = new RecurrentCallHandle(action, time, AssortedExtensions.PrimeNumberNearestToUInt32MaxValue, this);
#if VALIDATE_USAGE
        lock (RecurrentCallsSchedule)
            RecurrentCallsSchedule.Add(new(rch));
#else
        lock (RecurrentCallsSchedule)
            RecurrentCallsSchedule.Add(rch);
#endif

        return rch;
    }

#if VALIDATE_USAGE
    /// <summary>
    /// Schedules <paramref name="action"/> to run indefinitely every time <paramref name="frames"/> amount of frames elapse
    /// </summary>
    /// <param name="action">The action to call</param>
    /// <param name="frames">The amount of frames to wait each time before calling <paramref name="action"/></param>
    /// <returns>
    /// An <see cref="IDisposable"/> object that, when disposed, removes the recurrent call from the schedule. If this object is claimed by the GC before being disposed, the <see cref="DeferredExecutionSchedule"/> that produced this object will eventually throw an Exception, but finalization is non-deterministic, can be run at any time and is not guaranteed to run at all.
    /// </returns>
#else
    /// <summary>
    /// Schedules <paramref name="action"/> to run indefinitely every time <paramref name="frames"/> amount of frames elapses
    /// </summary>
    /// <param name="action">The action to call</param>
    /// <param name="time">The amount of frames to wait each time before calling <paramref name="action"/></param>
    /// <returns>
    /// An <see cref="IDisposable"/> object that, when disposed, removes the recurrent call from the schedule. If this object is claimed by the GC before being disposed, it will also result in the delegate being de-scheduled, but finalization is non-deterministic, can be run at any time and is not guaranteed to run at all.
    /// </returns>
#endif
    public IDisposable ScheduleRecurrentCall(UpdateEvent<DeferredExecutionSchedule> action, ushort frames)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (frames <= 0)
            throw new ArgumentOutOfRangeException(nameof(frames), "The amount of frames to defer action for must be larger than 0");

        var rch = new RecurrentCallHandle(action, TimeSpan.MaxValue, frames, this);
#if VALIDATE_USAGE
        lock (RecurrentCallsSchedule)
            RecurrentCallsSchedule.Add(rch.RefSelf);
#else
        lock (RecurrentCallsSchedule)
            RecurrentCallsSchedule.Add(rch);
#endif

        return rch;
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run for <paramref name="time"/> amount of time
    /// </summary>
    /// <param name="action">The action to call</param>
    /// <param name="time">The amount of time this call will be repeated (per-frame) for</param>
    public void ScheduleRepeatedCallFor(UpdateEvent<DeferredExecutionSchedule> action, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (time <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(time), "The amount of time to defer action for must be larger than 0");

        lock (TimedActionSchedule)
            TimedActionSchedule.AddLast(new TimedActionInfo(
                Action: action,
                Frames: AssortedExtensions.PrimeNumberNearestToUInt32MaxValue,
                Time: time + Watch.Elapsed
            ));
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run for <paramref name="frames"/> frames
    /// </summary>
    /// <param name="action">The action to call</param>
    /// <param name="frames">The amount of frames this call will be repeated for</param>
    public void ScheduleRepeatedCallFor(UpdateEvent<DeferredExecutionSchedule> action, ushort frames)
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
    public void ScheduleDeferredCall(UpdateEvent<DeferredExecutionSchedule> action, TimeSpan time)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (time <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(time), "The amount of time to defer action for must be larger than 0");

        lock (OneTimeSchedule)
            OneTimeSchedule.AddLast(new DeferredCallInfo(
                Action: action,
                Frames: AssortedExtensions.PrimeNumberNearestToUInt32MaxValue,
                Time: time + Watch.Elapsed,
                Registered: Watch.Elapsed
            ));
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run after <paramref name="frames"/> amount of frames
    /// </summary>
    /// <remarks>
    /// A frame in this context is defined as an update frame from this <see cref="DeferredExecutionSchedule"/>
    /// </remarks>
    /// <param name="action">The action to call</param>
    /// <param name="frames">The amount of frames to wait before calling <paramref name="action"/></param>
    public void ScheduleDeferredCall(UpdateEvent<DeferredExecutionSchedule> action, ushort frames)
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

    /// <summary>
    /// Represents a handle to a specific delegate that has been scheduled as a Recurrent Call in a <see cref="DeferredExecutionSchedule"/>
    /// </summary>
    private sealed class RecurrentCallHandle : IDisposable
    {
#if VALIDATE_USAGE
        internal readonly WeakReference<RecurrentCallHandle> RefSelf;
#endif
        private readonly DeferredExecutionSchedule Schedule;

        internal readonly UpdateEvent<DeferredExecutionSchedule> ActionRef;
        internal readonly TimeSpan Time;
        internal readonly uint Frames;

        internal RecurrentCallHandle(UpdateEvent<DeferredExecutionSchedule> actionRef, TimeSpan time, uint frames, DeferredExecutionSchedule schedule)
        {
            ActionRef = actionRef;
            Time = time;
            Frames = frames;
            Schedule = schedule;
#if VALIDATE_USAGE
            RefSelf = new(this);
#endif
        }

        private bool disposedValue;
        void IDisposable.Dispose()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(RecurrentCallHandle));
            disposedValue = true;
#if VALIDATE_USAGE
            Schedule.RecurrentCallsSchedule.Remove(RefSelf);
#else
            Schedule.RecurrentCallsSchedule.Remove(this);
#endif
            GC.SuppressFinalize(this);
        }
    }

    internal record struct TimedActionInfo(UpdateEvent<DeferredExecutionSchedule> Action, TimeSpan Time, uint Frames);

    internal record struct DeferredCallInfo(UpdateEvent<DeferredExecutionSchedule> Action, TimeSpan Time, uint Frames, TimeSpan Registered);

#endregion
}
