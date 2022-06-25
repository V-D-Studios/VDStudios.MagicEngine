using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using Timer = System.Timers.Timer;

namespace VDStudios.MagicEngine.Demo.Services;

public class TimedSequence<T>
{
    public bool Reverse { get; set; }
    public int PrevIndex { get; private set; }
    public int Index { get; private set; }
    public int NextIndex { get; private set; }
    public bool IsReversing { get; private set; }
    public ReadOnlyCollection<T> Items { get; private set; }

    private TimeSpan Buffer { get; set; }

    public T CurrentElement => TArray[Index];
    public double IntervalsPerSecond => 1 / Interval.TotalSeconds;

    public TimeSpan Interval
    {
        get => IntervalField;
        set
        {
            IntervalField = value;
            ResetTimer();
        }
    }
    private TimeSpan IntervalField;

    private T[] TArray
    {
        get => TArrayField;
        set => SetItems(value);
    }
    private T[] TArrayField;

    public Timer? Timer
    {
        get => TimerField;
        set
        {
            if (ReferenceEquals(TimerField, value))
                return;
            if (TimerField is not null)
            {
                TimerField.Elapsed -= Timer_Elapsed;
                TimerField.Dispose();
            }
            TimerField = value;
            if (TimerField is not null)
            {
                TimerField.Elapsed += Timer_Elapsed;
                TimerField.Interval = Interval.TotalMilliseconds;
                ResetTimer();
            }
        }
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e) { Step(); ResetTimer(); }

    private Timer? TimerField;

    public bool Autonomous
    {
        get => AutonomousField;
        set
        {
            if (AutonomousField == value)
                return;
            else Timer = value ? (new(Interval.TotalMilliseconds)) : null; //If value is true, then it was previously false, as it's not equal to its previous value
            AutonomousField = value;
        }
    }
    private bool AutonomousField = false;

    private void ResetTimer()
    {
        if (Timer is null)
            return;
        Timer.Stop();
        Timer.Interval = Interval.TotalMilliseconds;
        Timer.Start();
    }

    private (int index, bool reverse) NextStep() 
        => !IsReversing
            ? Index != TArray.Length - 1
                ? ((int index, bool reverse))(Index + 1, false)
                : Reverse ? ((int index, bool reverse))(Index, true) : ((int index, bool reverse))(0, false)
            : Index > 0 ? ((int index, bool reverse))(Index - 1, true) : ((int index, bool reverse))(Index + 1, false);

    public void Step()
    {
        var (ind, reverse) = NextStep();
        Index = ind;
        IsReversing = reverse;
        NextIndex = NextStep().index;
    }

    public void Update(TimeSpan span)
    {
        Buffer += span;
        if (Buffer >= Interval)
        {
            Buffer = TimeSpan.Zero;
            Step();
        }
    }

    public void SetItems(IEnumerable<T> items)
    {
        TArrayField = items.ToArray();
        Items = new(TArrayField);
        Buffer = TimeSpan.Zero;
        Index = 0;
        PrevIndex = 0;
    }

    public TimedSequence(float intervalsPerSecond, IEnumerable<T> items, bool autonomous = true) : this(TimeSpan.FromSeconds(1 / intervalsPerSecond), items, autonomous) { }
    public TimedSequence(TimeSpan interval, IEnumerable<T> items, bool autonomous = true)
    {
        TArrayField = items.ToArray();
        Items = new(TArrayField);
        Interval = interval;
        Autonomous = autonomous;
    }
}
