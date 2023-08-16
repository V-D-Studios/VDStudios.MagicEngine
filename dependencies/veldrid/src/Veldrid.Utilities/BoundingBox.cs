using System;
using System.Numerics;

namespace Veldrid.Utilities;

public struct BoundingBox : IEquatable<BoundingBox>
{
    public Vector3 Min;
    public Vector3 Max;

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public ContainmentType Contains(ref BoundingBox other)
    {
        if (Max.X < other.Min.X || Min.X > other.Max.X
            || Max.Y < other.Min.Y || Min.Y > other.Max.Y
            || Max.Z < other.Min.Z || Min.Z > other.Max.Z)
        {
            return ContainmentType.Disjoint;
        }
        else if (Min.X <= other.Min.X && Max.X >= other.Max.X
            && Min.Y <= other.Min.Y && Max.Y >= other.Max.Y
            && Min.Z <= other.Min.Z && Max.Z >= other.Max.Z)
        {
            return ContainmentType.Contains;
        }
        else
        {
            return ContainmentType.Intersects;
        }
    }

    public Vector3 GetCenter()
    {
        return (Max + Min) / 2f;
    }

    public Vector3 GetDimensions()
    {
        return Max - Min;
    }

    public static BoundingBox Transform(BoundingBox box, Matrix4x4 mat)
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        box.GetCorners(corners);

        Vector3 min = Vector3.Transform(corners[0], mat);
        Vector3 max = Vector3.Transform(corners[0], mat);

        for (int i = 1; i < 8; i++)
        {
            min = Vector3.Min(min, Vector3.Transform(corners[i], mat));
            max = Vector3.Max(max, Vector3.Transform(corners[i], mat));
        }

        return new BoundingBox(min, max);
    }

    public static unsafe BoundingBox CreateFromPoints(
        Span<Vector3> vertices,
        int vertexStride,
        Quaternion rotation,
        Vector3 offset,
        Vector3 scale)
    {
        fixed (Vector3* vertexPtr_0 = vertices)
        {
            Vector3* vertexPtr = vertexPtr_0;
            byte* bytePtr = (byte*)vertexPtr;
            Vector3 min = Vector3.Transform(*vertexPtr, rotation);
            Vector3 max = Vector3.Transform(*vertexPtr, rotation);

            for (int i = 1; i < vertices.Length; i++)
            {
                bytePtr += vertexStride;
                vertexPtr = (Vector3*)bytePtr;
                Vector3 pos = Vector3.Transform(*vertexPtr, rotation);

                if (min.X > pos.X) min.X = pos.X;
                if (max.X < pos.X) max.X = pos.X;

                if (min.Y > pos.Y) min.Y = pos.Y;
                if (max.Y < pos.Y) max.Y = pos.Y;

                if (min.Z > pos.Z) min.Z = pos.Z;
                if (max.Z < pos.Z) max.Z = pos.Z;
            }
            return new BoundingBox((min * scale) + offset, (max * scale) + offset);
        }
    }

    public static BoundingBox CreateFromVertices(Span<Vector3> vertices)
    {
        return CreateFromVertices(vertices, Quaternion.Identity, Vector3.Zero, Vector3.One);
    }

    public static BoundingBox CreateFromVertices(Span<Vector3> vertices, Quaternion rotation, Vector3 offset, Vector3 scale)
    {
        Vector3 min = Vector3.Transform(vertices[0], rotation);
        Vector3 max = Vector3.Transform(vertices[0], rotation);

        for (int i = 1; i < vertices.Length; i++)
        {
            Vector3 pos = Vector3.Transform(vertices[i], rotation);

            if (min.X > pos.X) min.X = pos.X;
            if (max.X < pos.X) max.X = pos.X;

            if (min.Y > pos.Y) min.Y = pos.Y;
            if (max.Y < pos.Y) max.Y = pos.Y;

            if (min.Z > pos.Z) min.Z = pos.Z;
            if (max.Z < pos.Z) max.Z = pos.Z;
        }

        return new BoundingBox((min * scale) + offset, (max * scale) + offset);
    }

    public static BoundingBox Combine(BoundingBox box1, BoundingBox box2)
    {
        return new BoundingBox(
            Vector3.Min(box1.Min, box2.Min),
            Vector3.Max(box1.Max, box2.Max));
    }

    public static bool operator ==(BoundingBox first, BoundingBox second)
    {
        return first.Equals(second);
    }

    public static bool operator !=(BoundingBox first, BoundingBox second)
    {
        return !first.Equals(second);
    }

    public bool Equals(BoundingBox other)
    {
        return Min == other.Min && Max == other.Max;
    }

    public override string ToString()
    {
        return string.Format("Min:{0}, Max:{1}", Min, Max);
    }

    public override bool Equals(object? obj)
    {
        return obj is BoundingBox box && box.Equals(this);
    }

    public override int GetHashCode()
    {
        int h1 = Min.GetHashCode();
        int h2 = Max.GetHashCode();
        uint shift5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
        return ((int)shift5 + h1) ^ h2;
    }

    public void GetCorners(Span<Vector3> alignedCorners)
    {
        if (alignedCorners.Length < 8)
            throw new ArgumentException("Input buffer must have a length of at least 8 elements", nameof(alignedCorners));
        alignedCorners[0] = new Vector3(Min.X, Max.Y, Max.Z);
        alignedCorners[1] = new Vector3(Max.X, Max.Y, Max.Z);
        alignedCorners[2] = new Vector3(Min.X, Min.Y, Max.Z);
        alignedCorners[3] = new Vector3(Max.X, Min.Y, Max.Z);

        alignedCorners[4] = new Vector3(Min.X, Max.Y, Min.Z);
        alignedCorners[5] = new Vector3(Max.X, Max.Y, Min.Z);
        alignedCorners[6] = new Vector3(Min.X, Min.Y, Min.Z);
        alignedCorners[7] = new Vector3(Max.X, Min.Y, Min.Z);
    }

    public AlignedBoxCorners GetCorners()
    {
        AlignedBoxCorners corners;
        corners.NearTopLeft = new Vector3(Min.X, Max.Y, Max.Z);
        corners.NearTopRight = new Vector3(Max.X, Max.Y, Max.Z);
        corners.NearBottomLeft = new Vector3(Min.X, Min.Y, Max.Z);
        corners.NearBottomRight = new Vector3(Max.X, Min.Y, Max.Z);

        corners.FarTopLeft = new Vector3(Min.X, Max.Y, Min.Z);
        corners.FarTopRight = new Vector3(Max.X, Max.Y, Min.Z);
        corners.FarBottomLeft = new Vector3(Min.X, Min.Y, Min.Z);
        corners.FarBottomRight = new Vector3(Max.X, Min.Y, Min.Z);

        return corners;
    }

    public bool ContainsNaN()
    {
        return float.IsNaN(Min.X) || float.IsNaN(Min.Y) || float.IsNaN(Min.Z)
            || float.IsNaN(Max.X) || float.IsNaN(Max.Y) || float.IsNaN(Max.Z);
    }
}
