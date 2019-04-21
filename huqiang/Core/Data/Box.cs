using System;
using System.Collections.Generic;
using UnityEngine;

namespace huqiang.Data
{
    public struct Box
    {
        public Vector3 center;
        public Vector3 size;
        public static bool Contains(ref Box abox, ref Box bbox)
        {
            float x = bbox.size.x * 0.5f;
            float right = bbox.center.x + x;
            float mx = abox.size.x * 0.5f;
            if (right > abox.center.x + mx)
                return false;
            float left = bbox.center.x - x;
            if (left < abox.center.x - mx)
                return false;

            float y = bbox.size.y * 0.5f;
            float up = bbox.center.y + y;
            float my = abox.size.y * 0.5f;
            if (up > abox.center.x + my)
                return false;
            float down = bbox.center.y - y;
            if (down < abox.center.y - my)
                return false;

            float z = bbox.size.z * 0.5f;
            float behind = bbox.center.z + z;
            float mz = abox.size.z * 0.5f;
            if (behind > abox.center.z + mz)
                return false;
            float front = bbox.center.z - z;
            if (front < abox.center.z - mz)
                return false;

            return true;
        }
        public static Box GetCenter(Vector3[] vert)
        {
            if (vert == null)
                return new Box();
            float xi = vert[0].x;
            float xx = vert[0].x;
            float yi = vert[0].y;
            float yx = vert[0].y;
            float zi = vert[0].z;
            float zx = vert[0].z;
            for (int i = 1; i < vert.Length; i++)
            {
                if (vert[i].x < xi)
                    xi = vert[i].x;
                else if (vert[i].x > xx)
                    xx = vert[i].x;
                if (vert[i].y < yi)
                    yi = vert[i].y;
                else if (vert[i].y > yx)
                    yx = vert[i].y;
                if (vert[i].z < zi)
                    zi = vert[i].z;
                else if (vert[i].z > zx)
                    zx = vert[i].z;
            }
            Box box = new Box();
            box.size.x = xx - xi;
            box.size.y = yx - yi;
            box.size.z = zx - zi;
            box.center.x = (xi + xx) * 0.5f;
            box.center.y = (yi + yx) * 0.5f;
            box.center.z = (zi + zx) * 0.5f;
            return box;
        }
        public static void ReLocation(Vector3[] vert, ref Vector3 location)
        {
            var box = GetCenter(vert);
            var v = location - box.center;
            for (int i = 0; i < vert.Length; i++)
                vert[i] += v;
            location = box.center;
        }

        struct Result
        {
            public float minX;
            public float minY;
            public float minZ;
            public float maxX;
            public float maxY;
            public float maxZ;
            public bool Frist;
        }
        public static Box GetMeshCenter(MeshData mesh)
        {
            Result r = new Result();
            GetMeshCenterS(mesh, Vector3.zero, Vector3.zero, Quaternion.identity, ref r);
            Box box = new Box();
            box.size.x = r.maxX - r.minX;
            box.size.y = r.maxY - r.minY;
            box.size.z = r.maxZ - r.minZ;
            box.center.x = (r.minX + r.maxX) * 0.5f;
            box.center.y = (r.minY + r.maxY) * 0.5f;
            box.center.z = (r.minZ + r.maxZ) * 0.5f;
            return box;
        }
        static void GetMeshCenterS(MeshData mesh, Vector3 pos, Vector3 scale, Quaternion q, ref Result r)
        {
            var vert = mesh.vertex;
            var a = q * mesh.coordinate.pos;
            var tops = a + pos;
            q *= mesh.coordinate.quat;
            var ls = mesh.coordinate.scale;
            scale.x *= ls.x;
            scale.y *= ls.y;
            scale.z *= ls.z;
            if (vert != null)
            {
                if (!r.Frist)
                {
                    var v = q * vert[0];
                    v.x *= scale.x;
                    v.y *= scale.y;
                    v.z *= scale.z;
                    var x = tops.x + v.x;
                    r.minX = x;
                    r.maxX = x;
                    var y = tops.y + v.y;
                    r.minY = y;
                    r.maxY = y;
                    var z = tops.z + v.z;
                    r.minZ = z;
                    r.maxZ = z;
                    r.Frist = true;
                }
                for (int i = 0; i < vert.Length; i++)
                {
                    var v = q * vert[i];
                    v.x *= ls.x;
                    v.y *= ls.y;
                    v.z *= ls.z;
                    var x = tops.x + v.x;
                    if (x < r.minX)
                        r.minX = x;
                    else if (x > r.maxX)
                        r.maxX = x;
                    var y = tops.y + v.y;
                    if (y < r.minY)
                        r.minY = y;
                    else if (y > r.maxY)
                        r.maxY = y;
                    var z = tops.z + v.z;
                    if (z < r.minZ)
                        r.minZ = z;
                    else if (z > r.maxZ)
                        r.maxZ = z;
                }
            }
            var child = mesh.child;
            if (child != null)
                for (int i = 0; i < child.Length; i++)
                    GetMeshCenterS(child[i], tops, scale, q, ref r);
        }
    }
}
