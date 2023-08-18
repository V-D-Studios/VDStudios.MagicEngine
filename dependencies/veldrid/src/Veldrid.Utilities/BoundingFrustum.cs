using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Veldrid.Utilities;

public struct BoundingFrustum
{
    private SixPlane _planes;

    private struct SixPlane
    {
        public Plane Left;
        public Plane Right;
        public Plane Bottom;
        public Plane Top;
        public Plane Near;
        public Plane Far;

        public void WritePlanes(Span<Plane> buffer)
        {
            if (buffer.Length < 6)
                throw new ArgumentException("The passed buffer must have a length of at least 6 elements", nameof(buffer));

            buffer[0] = Left;
            buffer[1] = Right;
            buffer[2] = Bottom;
            buffer[3] = Top;
            buffer[4] = Near;
            buffer[5] = Far;
        }
    }

    public BoundingFrustum(Matrix4x4 m)
    {
        // Plane computations: http://gamedevs.org/uploads/fast-extraction-viewing-frustum-planes-from-world-view-projection-matrix.pdf
        _planes.Left = Plane.Normalize(
            new Plane(
                m.M14 + m.M11,
                m.M24 + m.M21,
                m.M34 + m.M31,
                m.M44 + m.M41));

        _planes.Right = Plane.Normalize(
            new Plane(
                m.M14 - m.M11,
                m.M24 - m.M21,
                m.M34 - m.M31,
                m.M44 - m.M41));

        _planes.Bottom = Plane.Normalize(
            new Plane(
                m.M14 + m.M12,
                m.M24 + m.M22,
                m.M34 + m.M32,
                m.M44 + m.M42));

        _planes.Top = Plane.Normalize(
            new Plane(
                m.M14 - m.M12,
                m.M24 - m.M22,
                m.M34 - m.M32,
                m.M44 - m.M42));

        _planes.Near = Plane.Normalize(
            new Plane(
                m.M13,
                m.M23,
                m.M33,
                m.M43));

        _planes.Far = Plane.Normalize(
            new Plane(
                m.M14 - m.M13,
                m.M24 - m.M23,
                m.M34 - m.M33,
                m.M44 - m.M43));
    }

    public BoundingFrustum(Plane left, Plane right, Plane bottom, Plane top, Plane near, Plane far)
    {
        _planes.Left = left;
        _planes.Right = right;
        _planes.Bottom = bottom;
        _planes.Top = top;
        _planes.Near = near;
        _planes.Far = far;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContainmentType Contains(Vector3 point)
    {
        Span<Plane> planes = stackalloc Plane[6];
        _planes.WritePlanes(planes);

        for (int i = 0; i < 6; i++)
        {
            if (Plane.DotCoordinate(planes[i], point) < 0)
            {
                return ContainmentType.Disjoint;
            }
        }

        return ContainmentType.Contains;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContainmentType Contains(ref Vector3 point)
    {
        Span<Plane> planes = stackalloc Plane[6];
        _planes.WritePlanes(planes);

        for (int i = 0; i < 6; i++)
        {
            if (Plane.DotCoordinate(planes[i], point) < 0)
            {
                return ContainmentType.Disjoint;
            }
        }

        return ContainmentType.Contains;
    }

    public ContainmentType Contains(BoundingSphere sphere)
    {
        Span<Plane> planes = stackalloc Plane[6];
        _planes.WritePlanes(planes);

        ContainmentType result = ContainmentType.Contains;
        for (int i = 0; i < 6; i++)
        {
            float distance = Plane.DotCoordinate(planes[i], sphere.Center);
            if (distance < -sphere.Radius)
            {
                return ContainmentType.Disjoint;
            }
            else if (distance < sphere.Radius)
            {
                result = ContainmentType.Intersects;
            }
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContainmentType Contains(BoundingBox box) => Contains(ref box);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContainmentType Contains(ref BoundingBox box)
    {
        Span<Plane> planes = stackalloc Plane[6];
        _planes.WritePlanes(planes);

        ContainmentType result = ContainmentType.Contains;
        for (int i = 0; i < 6; i++)
        {
            Plane plane = planes[i];

            // Approach: http://zach.in.tu-clausthal.de/teaching/cg_literatur/lighthouse3d_view_frustum_culling/index.html

            Vector3 positive = new Vector3(box.Min.X, box.Min.Y, box.Min.Z);
            Vector3 negative = new Vector3(box.Max.X, box.Max.Y, box.Max.Z);

            if (plane.Normal.X >= 0)
            {
                positive.X = box.Max.X;
                negative.X = box.Min.X;
            }
            if (plane.Normal.Y >= 0)
            {
                positive.Y = box.Max.Y;
                negative.Y = box.Min.Y;
            }
            if (plane.Normal.Z >= 0)
            {
                positive.Z = box.Max.Z;
                negative.Z = box.Min.Z;
            }

            // If the positive vertex is outside (behind plane), the box is disjoint.
            float positiveDistance = Plane.DotCoordinate(plane, positive);
            if (positiveDistance < 0)
            {
                return ContainmentType.Disjoint;
            }

            // If the negative vertex is outside (behind plane), the box is intersecting.
            // Because the above check failed, the positive vertex is in front of the plane,
            // and the negative vertex is behind. Thus, the box is intersecting this plane.
            float negativeDistance = Plane.DotCoordinate(plane, negative);
            if (negativeDistance < 0)
            {
                result = ContainmentType.Intersects;
            }
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ContainmentType Contains(ref BoundingFrustum other)
    {
        int pointsContained = 0;
        Span<Vector3> corners = stackalloc Vector3[8];
        other.GetCorners(corners);

        for (int i = 0; i < 8; i++)
        {
            if (Contains(ref corners[i]) != ContainmentType.Disjoint)
            {
                pointsContained++;
            }
        }

        return pointsContained == 8 ? ContainmentType.Contains : pointsContained == 0 ? ContainmentType.Disjoint : ContainmentType.Intersects;
    }

    public FrustumCorners GetCorners()
    {
        FrustumCorners corners;
        GetCorners(out corners);
        return corners;
    }

    public void GetCorners(out FrustumCorners corners)
    {
        PlaneIntersection(ref _planes.Near, ref _planes.Top, ref _planes.Left, out corners.NearTopLeft);
        PlaneIntersection(ref _planes.Near, ref _planes.Top, ref _planes.Right, out corners.NearTopRight);
        PlaneIntersection(ref _planes.Near, ref _planes.Bottom, ref _planes.Left, out corners.NearBottomLeft);
        PlaneIntersection(ref _planes.Near, ref _planes.Bottom, ref _planes.Right, out corners.NearBottomRight);
        PlaneIntersection(ref _planes.Far, ref _planes.Top, ref _planes.Left, out corners.FarTopLeft);
        PlaneIntersection(ref _planes.Far, ref _planes.Top, ref _planes.Right, out corners.FarTopRight);
        PlaneIntersection(ref _planes.Far, ref _planes.Bottom, ref _planes.Left, out corners.FarBottomLeft);
        PlaneIntersection(ref _planes.Far, ref _planes.Bottom, ref _planes.Right, out corners.FarBottomRight);
    }

    public void GetCorners(Span<Vector3> corners)
    {
        if (corners.Length < 8)
            throw new ArgumentException("The passed buffer must have a length of 8 elements or more", nameof(corners));
        PlaneIntersection(ref _planes.Near, ref _planes.Top, ref _planes.Left, out corners[0]);
        PlaneIntersection(ref _planes.Near, ref _planes.Top, ref _planes.Right, out corners[1]);
        PlaneIntersection(ref _planes.Near, ref _planes.Bottom, ref _planes.Left, out corners[2]);
        PlaneIntersection(ref _planes.Near, ref _planes.Bottom, ref _planes.Right, out corners[3]);
        PlaneIntersection(ref _planes.Far, ref _planes.Top, ref _planes.Left, out corners[4]);
        PlaneIntersection(ref _planes.Far, ref _planes.Top, ref _planes.Right, out corners[5]);
        PlaneIntersection(ref _planes.Far, ref _planes.Bottom, ref _planes.Left, out corners[6]);
        PlaneIntersection(ref _planes.Far, ref _planes.Bottom, ref _planes.Right, out corners[7]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PlaneIntersection(ref Plane p1, ref Plane p2, ref Plane p3, out Vector3 intersection)
    {
        // Formula: http://geomalgorithms.com/a05-_intersect-1.html
        // The formula assumes that there is only a single intersection point.
        // Because of the way the frustum planes are constructed, this should be guaranteed.
        intersection =
            (-(p1.D * Vector3.Cross(p2.Normal, p3.Normal))
            - (p2.D * Vector3.Cross(p3.Normal, p1.Normal))
            - (p3.D * Vector3.Cross(p1.Normal, p2.Normal)))
            / Vector3.Dot(p1.Normal, Vector3.Cross(p2.Normal, p3.Normal));
    }
}
