using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a source of data for an <see cref="ReferenceData{T}"/> object
/// </summary>
/// <remarks>
/// This class is useful to propagate large structs into other objects, or to present mutable data throughout multiple elements without releasing control of it. This object should be stored and mutated as necessary 
/// </remarks>
public sealed class ReferenceDataSource<T> where T : notnull
{
    private readonly object sync = new();

    /// <summary>
    /// Instances a new object of type <see cref="ReferenceDataSource{T}"/>
    /// </summary>
    public ReferenceDataSource(T data)
    {
        InmutableData = new(this);
        _dat = data;
    }

    /// <summary>
    /// A <see cref="ReferenceData{T}"/> object that reflects the data held by this object
    /// </summary>
    public ReferenceData<T> InmutableData { get; }

    /// <summary>
    /// The data held by this <see cref="ReferenceDataSource{T}"/>, and that will be reflected on <see cref="InmutableData"/> as a read-only member upon changing
    /// </summary>
    public T Data
    {
        get => _dat;
        set => _dat = value;
    }

    /// <summary>
    /// The data held by this <see cref="ReferenceDataSource{T}"/>, and that will be reflected on <see cref="InmutableData"/> as a read-only member upon changing
    /// </summary>
    /// <remarks>
    /// <see cref="ConcurrentData"/> and <see cref="ReferenceData{T}.ConcurrentData"/> lock from each other, but this does not affect <see cref="DataRef"/>, <see cref="Data"/>, or <see cref="ReferenceData{T}.Data"/>
    /// </remarks>
    public T ConcurrentData
    {
        get
        {
            lock (sync)
                return _dat;
        }
        set
        {
            lock (sync)
                _dat = value;
        }
    }

    /// <summary>
    /// A reference to the storage of the data held by this <see cref="ReferenceDataSource{T}"/>, and that will be reflected on <see cref="InmutableData"/> as a read-only member upon changing
    /// </summary>
    public ref T DataRef => ref _dat;

    private T _dat;
}
