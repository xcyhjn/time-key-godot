using UnityEngine;

namespace TimeKey.Presentation
{
    internal static class HexMeshFactory
    {
        public static Mesh Create(float radius, float height)
        {
            var vertices = new Vector3[12];
            for (var index = 0; index < 6; index++)
            {
                var angle = Mathf.Deg2Rad * (60f * index);
                var x = radius * Mathf.Cos(angle);
                var z = radius * Mathf.Sin(angle);
                vertices[index] = new Vector3(x, height, z);
                vertices[index + 6] = new Vector3(x, 0f, z);
            }

            var triangles = new int[60];
            var cursor = 0;
            for (var index = 1; index < 5; index++)
            {
                triangles[cursor++] = 0;
                triangles[cursor++] = index;
                triangles[cursor++] = index + 1;
                triangles[cursor++] = 6;
                triangles[cursor++] = index + 7;
                triangles[cursor++] = index + 6;
            }

            for (var index = 0; index < 6; index++)
            {
                var next = (index + 1) % 6;
                triangles[cursor++] = index;
                triangles[cursor++] = next;
                triangles[cursor++] = index + 6;
                triangles[cursor++] = next;
                triangles[cursor++] = next + 6;
                triangles[cursor++] = index + 6;
            }

            var mesh = new Mesh { name = "TimeKey Hex" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
