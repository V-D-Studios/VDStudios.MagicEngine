using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Veldrid.Utilities
{
    public class SimpleMeshDataProvider : MeshData
    {
        public VertexPositionNormalTexture[] Vertices { get; }
        public ushort[] Indices { get; }
        public string MaterialName { get; }

        public SimpleMeshDataProvider(VertexPositionNormalTexture[] vertices, ushort[] indices)
        {
            Vertices = vertices;
            Indices = indices;
        }

        public DeviceBuffer CreateVertexBuffer(ResourceFactory factory, CommandList cl)
        {
            DeviceBuffer vb = factory.CreateBuffer(
                new BufferDescription(
                    (uint)(Vertices.Length * VertexPositionNormalTexture.SizeInBytes),
                    BufferUsage.VertexBuffer));
            cl.UpdateBuffer(vb, 0, Vertices);
            return vb;
        }

        public DeviceBuffer CreateIndexBuffer(ResourceFactory factory, CommandList cl, out int indexCount)
        {
            DeviceBuffer ib = factory.CreateBuffer(new BufferDescription((uint)(Indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
            cl.UpdateBuffer(ib, 0, Indices);
            indexCount = Indices.Length;
            return ib;
        }

        public unsafe BoundingSphere GetBoundingSphere()
        {
            fixed (void* ptr = Vertices)
            {
                return BoundingSphere.CreateFromPoints((Vector3*)ptr, Vertices.Length, VertexPositionNormalTexture.SizeInBytes);
            }
        }

        public unsafe BoundingBox GetBoundingBox()
        {
            fixed (void* ptr = Vertices)
            {
                Span<Vector3> span = new(ptr, Vertices.Length);
                return BoundingBox.CreateFromPoints(
                    span,
                    VertexPositionNormalTexture.SizeInBytes,
                    Quaternion.Identity,
                    Vector3.Zero,
                    Vector3.One);
            }
        }

        public bool RayCast(Ray ray, out float distance)
        {
            distance = float.MaxValue;
            bool result = false;
            for (int i = 0; i < Indices.Length - 2; i += 3)
            {
                Vector3 v0 = Vertices[Indices[i + 0]].Position;
                Vector3 v1 = Vertices[Indices[i + 1]].Position;
                Vector3 v2 = Vertices[Indices[i + 2]].Position;

                float newDistance;
                if (ray.Intersects(ref v0, ref v1, ref v2, out newDistance))
                {
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                    }

                    result = true;
                }
            }

            return result;
        }

        public int RayCast(Ray ray, List<float> distances)
        {
            int hits = 0;
            for (int i = 0; i < Indices.Length - 2; i += 3)
            {
                Vector3 v0 = Vertices[Indices[i + 0]].Position;
                Vector3 v1 = Vertices[Indices[i + 1]].Position;
                Vector3 v2 = Vertices[Indices[i + 2]].Position;

                float newDistance;
                if (ray.Intersects(ref v0, ref v1, ref v2, out newDistance))
                {
                    hits++;
                    distances.Add(newDistance);
                }
            }

            return hits;
        }

        public void GetVertexPositions(Span<Vector3> buffer)
        {
            var vert = Vertices;
            if (buffer.Length < vert.Length)
                throw new ArgumentException("The length of the buffer must be equal or larger than the length of the Vertices array", nameof(buffer));
            for (int i = 0; i < vert.Length; i++) buffer[i] = vert[i].Position;
        }

        public void GetIndices(Span<ushort> buffer)
        {
            var indices = Indices;
            if (buffer.Length < indices.Length)
                throw new ArgumentException("The length of the buffer must be equal or larger than the length of the Indices array", nameof(buffer));
            for (int i = 0; i < indices.Length; i++) buffer[i] = indices[i];
        }
    }
}
