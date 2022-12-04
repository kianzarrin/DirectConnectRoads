using ColossalFramework.Math;
using KianCommons;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DirectConnectRoads.Util {
    public static class RoadMeshUtil {
        //public const float ASPHALT_HEIGHT = -.3f;

        static bool EqualApprox(float a, float b) {
            var diff = a - b;
            const float e = 1e-3f;
            return diff * diff < e * e;
        }

        static bool Contains(List<Vector2> list, Vector2 vector) {
            foreach (var item in list) {
                if (item == vector)
                    return true;
            }
            return false;
        }

        static bool Contains(List<float> list, float val, float e=1e-3f) {
            foreach (var item in list) {
                if (Mathf.Abs(item - val) <= e)
                    return true;
            }
            return false;
        }

        public static List<Vector2> CrossSection(this Mesh mesh) {
            var ret = new List<Vector2>(mesh.vertexCount);
            foreach (var vertex in mesh.vertices) {
                Vector2 vector2 = VectorUtils.XY(vertex);
                if (Contains(ret, vector2))
                    continue;
                ret.Add(vector2);
            }
            ret.Sort((lhs, rhs) => System.Math.Sign(lhs.x - rhs.x));
            return ret;
        }


        // TODO check if road is straight
        public static List<float> GetAsphaltEdges(this Mesh mesh, float voffset/* = ASPHALT_HEIGHT*/) {
            List<float> ret = new List<float>();
            var crossSection = mesh.CrossSection().ToArray();

            bool prevIsMedian = true;
            foreach( var v in crossSection) { 
                if (!EqualApprox(v.y, voffset))
                    continue;
                if (!prevIsMedian) // if both points are at road level then it does not form an edge.
                    continue;
                prevIsMedian = false;
                ret.Add(v.x);
            }

            return ret;
        }

        public static bool IsBetween(float val, float lower, float upper, float e = 1e-6f)
            => lower - e < val && val < upper + e;

        public static bool Selector(Vector3 vertex, float lower, float upper, bool asphalt, float voffset/* = ASPHALT_HEIGHT*/) {
            if (asphalt && !EqualApprox(vertex.y, voffset))
                return false;
            return IsBetween(vertex.x, lower, upper);
        }

        public static List<Mesh> GenerateSections(this Mesh mesh, out List<float> edges, float voffset) {
            edges = mesh.GetAsphaltEdges(voffset);
            int n = edges.Count - 1;
            var ret = new List<Mesh>(n);
            bool median = false;

            for(int i = 0; i < n; ++i) {
                float left = edges[i];
                float right = edges[i + 1];
                bool IsGoodFunc(Vector3 vertex) => Selector(vertex, left, right, asphalt:!median, voffset: voffset);
                var section = mesh.CutMeshGeneric2(IsGoodFunc);
                ret.Add(section);
            }
            return ret;
        }


        public static bool GetRoadSides(this Mesh mesh, out float left, out float right, float voffset/* = ASPHALT_HEIGHT*/) {
            left = right = float.NaN;

            var crossSection = mesh.CrossSection().ToArray();
            Log.Debug($"cross section for mesh:{mesh} is " + crossSection.ToSTR<Vector2>("f4"),false);

            for (int i = 1; i <= crossSection.Length / 2; ++i) {
                //Log.Debug("assesing vertex " + crossSection[i].ToString("e"),false);
                if (EqualApprox(crossSection[i].y, voffset)) {
                    left = crossSection[i].x;
                    break;
                }
            }

            crossSection = crossSection.Reverse().ToArray();
            for (int i = 1; i <= crossSection.Length / 2; ++i) {
                if (EqualApprox(crossSection[i].y, voffset)) {
                    right = crossSection[i].x;
                    break;
                }
            }
            Log.Debug($"left={left} right={right}",false);
            return !float.IsNaN(left) && !float.IsNaN(right);
        }

        public static bool IsMedianMesh(this Mesh mesh, float hw) {
            float left = mesh.vertices.Min(vertex => vertex.x);
            float right = mesh.vertices.Max(vertex => vertex.x);
            return right - left < hw;
        }

        /// <summary>
        /// returns a new mesh without the road sides.
        /// </summary>
        public static Mesh CutOutRoadSides(this Mesh mesh, float voffset, float hw) {
            if (mesh.GetRoadSides(out float left, out float right, voffset)) {
                bool IsGoodFunc(Vector3 vertex) {
                    float x = vertex.x;
                    if (EqualApprox(x, left) || EqualApprox(x, right))
                        return EqualApprox(vertex.y, voffset);
                    return left < vertex.x && vertex.x < right;
                }
                return mesh.CutMeshGeneric2(IsGoodFunc);
            }else if (mesh.IsMedianMesh(hw)) {
                return mesh;
            } else {
                return null;
            }
        }

        /// <summary>
        /// to avoid flickering, we elevate the DC mesh a bit.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="delta"></param>
        public static void Elevate(this Mesh mesh, float delta = 1e-2f) {
            var vertices = mesh.vertices;
            for (int i = 0; i < mesh.vertexCount; ++i)
                vertices[i].y += delta;

            mesh.vertices = vertices;
            mesh.triangles = mesh.triangles; // triangles must be set last // redundant ?
        }

    }
}
