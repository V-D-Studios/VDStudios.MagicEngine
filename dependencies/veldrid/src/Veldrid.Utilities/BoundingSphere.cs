﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace Veldrid.Utilities;

public struct BoundingSphere
{
    public Vector3 Center;
    public float Radius;

    public BoundingSphere(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public override string ToString()
    {
        return string.Format("Center:{0}, Radius:{1}", Center, Radius);
    }

    public bool Contains(Vector3 point)
    {
        return (Center - point).LengthSquared() <= Radius * Radius;
    }

    public static BoundingSphere CreateFromPoints(IList<Vector3> points)
    {
        Vector3 center = Vector3.Zero;
        foreach (Vector3 pt in points)
        {
            center += pt;
        }

        center /= points.Count;

        float maxDistanceSquared = 0f;
        foreach (Vector3 pt in points)
        {
            float distSq = Vector3.DistanceSquared(center, pt);
            if (distSq > maxDistanceSquared)
            {
                maxDistanceSquared = distSq;
            }
        }

        return new BoundingSphere(center, float.Sqrt(maxDistanceSquared));
    }

    public static unsafe BoundingSphere CreateFromPoints(Span<Vector3> points, int stride)
    {
        Vector3 center = Vector3.Zero;
        fixed (Vector3* pointPtr = points)
        {
            StrideHelper<Vector3> helper = new StrideHelper<Vector3>(pointPtr, points.Length, stride);
            foreach (Vector3 pos in helper)
                center += pos;
            center /= points.Length;

            float maxDistanceSquared = 0f;
            foreach (Vector3 pos in helper)
            {
                float distSq = Vector3.DistanceSquared(center, pos);
                if (distSq > maxDistanceSquared)
                {
                    maxDistanceSquared = distSq;
                }
            }

            return new BoundingSphere(center, float.Sqrt(maxDistanceSquared));
        }
    }
}
