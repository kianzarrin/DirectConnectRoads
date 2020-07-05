using ColossalFramework.Math;
using ObjUnity3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using UnityEngine;

namespace HideUnconnectedTracks.Utils {
    public static class MeshUtil {
        public static Mesh LoadMesh(string fileName) {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            var stream = executingAssembly.GetManifestResourceStream("HideUnconnectedTracks.Resources." + fileName);
            var mesh = new Mesh();
            mesh.LoadOBJ(OBJLoader.LoadOBJ(stream));
            return mesh;
        }

        public static Mesh CutMesh(this Mesh mesh, bool keepLeftSide) {
            const int EMPTY = -2;

            #region Calculate triangles and indexes
            var indexArray = new int[mesh.vertexCount];
            for (int i = 0; i < indexArray.Length; ++i) {
                const float EPSILON = 0.0001f;
                bool goodSide = keepLeftSide ? mesh.vertices[i].x < 0 + EPSILON : mesh.vertices[i].x > 0 - EPSILON;
                if (goodSide)
                    indexArray[i] = i;
                else
                    indexArray[i] = EMPTY; // mark for removal
            }
            indexArray = indexArray.Where(v => v != EMPTY).ToArray();

            // create inverse array
            int[] indexArrayInverse = new int[mesh.vertexCount];
            for (int i = 0; i < indexArrayInverse.Length; ++i) indexArrayInverse[i] = EMPTY;

            // switch value and index.
            for (int i = 0; i < indexArray.Length; ++i) {
                indexArrayInverse[indexArray[i]] = i;
            }

            // update triangle indeces and add only if all their vertices is on the right side.
            var newTriangleList = new List<int>(mesh.triangles.Length);
            for (var i = 0; i < mesh.triangles.Length; i += 3) {
                int newTriangle0 = indexArrayInverse[mesh.triangles[i]];
                int newTriangle1 = indexArrayInverse[mesh.triangles[i + 1]];
                int newTriangle2 = indexArrayInverse[mesh.triangles[i + 2]];
                if (newTriangle0 != EMPTY && newTriangle1 != EMPTY && newTriangle2 != EMPTY) {
                    newTriangleList.Add(newTriangle0);
                    newTriangleList.Add(newTriangle1);
                    newTriangleList.Add(newTriangle2);
                }
            }
            #endregion

            var newVertices = new Vector3[indexArray.Length];
            var newUV = new Vector2[indexArray.Length];
            var newNormals = new Vector3[indexArray.Length];
            var newTangents = new Vector4[indexArray.Length];
            for (int i = 0; i < indexArray.Length; ++i) {
                int j = indexArray[i];
                newVertices[i] = mesh.vertices[j];
                newUV[i] = mesh.uv[i];
                newNormals[i] = mesh.normals[j];
                newTangents[i] = mesh.tangents[j];
            }
            var newMesh = new Mesh();
            newMesh.bounds = mesh.bounds;
            newMesh.vertices = newVertices;
            newMesh.normals = newNormals;
            newMesh.tangents = newTangents;
            newMesh.triangles = newTriangleList.ToArray(); // triangle must be added after vertices.
            return newMesh;
        }

        public static Mesh CutMesh2(this Mesh mesh, bool keepLeftSide) {
            List<int> indexList = new List<int>(mesh.vertexCount);
            for (int i = 0; i < indexList.Count; ++i) indexList[i] = i; // create linear list

            // add only triangles on the right side.
            var newTriangleList = new List<int>(mesh.triangles.Length);
            for (var i = 0; i < mesh.triangles.Length; i += 3) {
                const float EPSILON = 0.0001f;
                bool GoodSide(int _i) {
                    var t = mesh.triangles[_i];
                    return keepLeftSide ? mesh.vertices[t].x < 0 + EPSILON : mesh.vertices[t].x > 0 - EPSILON;
                }
                if (GoodSide(i) && GoodSide(i + 1) && GoodSide(i + 2)) {
                    //Log._Debug($"Adding triangle[i:i+2]i={i}");
                    newTriangleList.Add(mesh.triangles[i]);
                    newTriangleList.Add(mesh.triangles[i + 1]);
                    newTriangleList.Add(mesh.triangles[i + 2]);
                }
            }

            var newMesh = new Mesh();
            newMesh.bounds = mesh.bounds;
            newMesh.vertices = mesh.vertices.ToArray();
            newMesh.uv = mesh.uv.ToArray();
            newMesh.normals = mesh.normals.ToArray();
            newMesh.tangents = mesh.tangents.ToArray();
            newMesh.triangles = newTriangleList.ToArray();
            return newMesh;
        }

        public delegate bool IsGoodHandler(Vector3 vertex);
        public static Mesh CutMeshGeneric2(this Mesh mesh, IsGoodHandler IsGoodFunc) {
            List<int> indexList = new List<int>(mesh.vertexCount);
            for (int i = 0; i < indexList.Count; ++i) indexList[i] = i; // create linear list

            // add only triangles on the right side.
            var newTriangleList = new List<int>(mesh.triangles.Length);
            for (var i = 0; i < mesh.triangles.Length; i += 3) {
                bool IsGood(int _i) {
                    var t = mesh.triangles[_i];
                    return IsGoodFunc(mesh.vertices[t]);
                }
                if (IsGood(i) && IsGood(i + 1) && IsGood(i + 2)) {
                    //Log._Debug($"Adding triangle[i:i+2]i={i}");
                    newTriangleList.Add(mesh.triangles[i]);
                    newTriangleList.Add(mesh.triangles[i + 1]);
                    newTriangleList.Add(mesh.triangles[i + 2]);
                }
            }

            var newMesh = new Mesh();
            newMesh.bounds = mesh.bounds;
            newMesh.vertices = mesh.vertices.ToArray();
            newMesh.uv = mesh.uv.ToArray();
            newMesh.normals = mesh.normals.ToArray();
            newMesh.tangents = mesh.tangents.ToArray();
            newMesh.triangles = newTriangleList.ToArray(); // triangle must be added after vertices.
            return newMesh;
        }

        public static Mesh CutMeshGeneric(this Mesh mesh, IsGoodHandler IsGoodFunc) {
            const int EMPTY = -2;

            #region Calculate triangles and indexes
            var indexArray = new int[mesh.vertexCount];
            for (int i = 0; i < indexArray.Length; ++i) {
                bool good = IsGoodFunc(mesh.vertices[i]);
                indexArray[i] = good ? i : EMPTY /*mark for removal*/;
            }
            indexArray = indexArray.Where(v => v != EMPTY).ToArray();

            // create inverse array
            int[] indexArrayInverse = new int[mesh.vertexCount];
            for (int i = 0; i < indexArrayInverse.Length; ++i) indexArrayInverse[i] = EMPTY;

            // switch value and index.
            for (int i = 0; i < indexArray.Length; ++i) {
                indexArrayInverse[indexArray[i]] = i;
            }

            // update triangle indeces and add only if all their vertices is on the right side.
            var newTriangleList = new List<int>(mesh.triangles.Length);
            for (var i = 0; i < mesh.triangles.Length; i += 3) {
                int newTriangle0 = indexArrayInverse[mesh.triangles[i]];
                int newTriangle1 = indexArrayInverse[mesh.triangles[i + 1]];
                int newTriangle2 = indexArrayInverse[mesh.triangles[i + 2]];
                if (newTriangle0 != EMPTY && newTriangle1 != EMPTY && newTriangle2 != EMPTY) {
                    newTriangleList.Add(newTriangle0);
                    newTriangleList.Add(newTriangle1);
                    newTriangleList.Add(newTriangle2);
                }
            }
            #endregion

            var newVertices = new Vector3[indexArray.Length];
            var newUV = new Vector2[indexArray.Length];
            var newNormals = new Vector3[indexArray.Length];
            var newTangents = new Vector4[indexArray.Length];
            for (int i = 0; i < indexArray.Length; ++i) {
                int j = indexArray[i];
                newVertices[i] = mesh.vertices[j];
                newUV[i] = mesh.uv[i];
                newNormals[i] = mesh.normals[j];
                newTangents[i] = mesh.tangents[j];
            }
            var newMesh = new Mesh();
            newMesh.bounds = mesh.bounds;
            newMesh.vertices = newVertices;
            newMesh.normals = newNormals;
            newMesh.tangents = newTangents;
            newMesh.triangles = newTriangleList.ToArray(); // triangle must be added after vertices.
            return newMesh;
        }


        static bool EqualApprox(float a, float b) {
            var diff = a - b;
            const float e = 1e-6f;
            return diff * diff < e * e;
        }

        static bool Contains(List<Vector2> list, Vector2 vector) {
            foreach(var item in list) {
                if (item == vector)
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
            ret.Sort((lhs, rhs) => Math.Sign(lhs.x - rhs.x));
            return ret;
        }

        public static bool GetRoadSides(this Mesh mesh, out float left, out float right) {
            left = right = float.NaN;

            var crossSection = mesh.CrossSection().ToArray();
            for (int i= 1; i <= crossSection.Length/2; ++i){
                if (EqualApprox(crossSection[i].y, -0.3f)){
                    left = crossSection[i].x;
                    break;
                }
            }

            crossSection = crossSection.Reverse().ToArray();
            for (int i = 1; i <= crossSection.Length / 2; ++i) {
                if (EqualApprox(crossSection[i].y, -0.3f)) {
                    right = crossSection[i].x;
                    break;
                }
            }

            return !float.IsNaN(left) && !float.IsNaN(right);
        }

        /// <summary>
        /// returns a new mesh without the road sides.
        /// </summary>
        public static Mesh CutOutRoadSides(this Mesh mesh) {
            if (!mesh.GetRoadSides(out float left, out float right))
                return null;
            return mesh.CutMeshGeneric2(vertex => left <= vertex.x && vertex.x <= right);
        }
    }
}
