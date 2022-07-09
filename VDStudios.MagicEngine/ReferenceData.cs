﻿namespace VDStudios.MagicEngine;

/// <summary>
/// Represents context of Transformation parameters for drawing. This class cannot be inherited or instantiated by user code, see <see cref="ReferenceDataSource{T}.InmutableData"/>
/// </summary>
/// <remarks>
/// This class provides a read-only view of data set by the <see cref="ReferenceDataSource{T}"/> that owns this object
/// </remarks>
public sealed class ReferenceData<T> where T : notnull
{
    private readonly ReferenceDataSource<T> Source;

    internal ReferenceData(ReferenceDataSource<T> source)
    {
        Source = source;
    }

    /// <summary>
    /// The data held by this <see cref="ReferenceData{T}"/>'s <see cref="ReferenceDataSource{T}"/>
    /// </summary>
    public T Data => Source.Data;

    /// <summary>
    /// The data held by this <see cref="ReferenceData{T}"/>'s <see cref="ReferenceDataSource{T}"/>
    /// </summary>
    /// <remarks>
    /// <see cref="ReferenceDataSource{T}.ConcurrentData"/> and <see cref="ConcurrentData"/> lock from each other, but this does not affect <see cref="ReferenceDataSource{T}.DataRef"/>, <see cref="ReferenceDataSource{T}.Data"/>, or <see cref="ReferenceDataSource{T}.Data"/>
    /// </remarks>
    public T ConcurrentData => Source.ConcurrentData;
}