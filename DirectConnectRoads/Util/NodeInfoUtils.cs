// TODO handle multiple NetInfo.segment .
namespace DirectConnectRoads {
    using KianCommons;
    using DirectConnectRoads.Util;
    using System.Collections.Generic;
    using UnityEngine;

    // [Serializable]
    public static class NodeInfoUtil{
        /// <summary>
        /// creates new clone of the node.
        /// </summary>
        public static NetInfo.Node Copy(NetInfo.Node template) {
            NetInfo.Node node = new NetInfo.Node();
            AssemblyTypeExtensions.CopyProperties<NetInfo.Node>(node, template);
            return node;
        }


        static bool IsSegmentInfoSuitable(NetInfo.Segment segmentInfo) {
            if (segmentInfo == null) return false;
            if (!segmentInfo.m_mesh || !segmentInfo.m_material)
                return false;
            if (!segmentInfo.m_material.TryGetTexture2D(TextureUtils.ID_Defuse))
                return false;
            return segmentInfo.CheckFlags(NetSegment.Flags.None, out _);
        }

        public static IEnumerable<NetInfo.Node> CreateDCNodes(NetInfo.Node template, NetInfo netInfo, float voffset/* = NetInfoUtil.ASPHALT_HEIGHT*/) {
            Assertion.AssertNotNull(netInfo, "netInfo");
            Log.Debug($"CreateDCNode({netInfo},{voffset}) called", false);
            var ret = new List<NetInfo.Node>();
            for (int i = 0; i < netInfo.m_segments.Length; ++i) {
                var segmentInfo = netInfo.m_segments[i];
                if (!IsSegmentInfoSuitable(segmentInfo)) {
                    Log.Debug($"Skiping segment[{i}]",false);
                    continue;
                }
                Log.Debug($"processing segment[{i}]", false);
                var material = new Material(segmentInfo.m_material);
                var mesh = segmentInfo.m_mesh;
                //Log.Debug("[1] mesh=" + mesh?.name ?? "null", false);
                mesh = mesh?.CutOutRoadSides(voffset);
                mesh?.Elevate();
                if (mesh == null || material == null) {
                    Log.Debug("CreateDCNode failed for " + netInfo.name, false);
                    continue;
                }

                mesh.name += "_DC";
                material.name += "_DC";
                //{
                //    var c = material.color;
                //    c.a = 0.01f;
                //    material.color = c;

                //    var tex = material.TryGetTexture2D(TextureUtils.ID_Defuse);
                //    tex = tex.GetReadableCopy();
                //    tex.Fade(0.1f);
                //    material.SetTexture(TextureUtils.ID_Defuse, tex);
                //}


                NetInfo.Node node = Copy(template);
                node.m_mesh = node.m_nodeMesh = mesh;
                node.m_material = node.m_nodeMaterial = material;
                node.m_directConnect = true;
                node.m_connectGroup = NetInfo.ConnectGroup.DoubleTrain;
                node.m_emptyTransparent = true;

                Log.Debug("CreateDCNode sucessful for " + netInfo.name, false);
                node.m_nodeMesh.DumpMesh($"DC mesh for {netInfo.name}");
                ret.Add(node);
            }
            return ret;

        }

        public static NetInfo.Node[] AddNode(NetInfo.Node[] nodeArray, NetInfo.Node node) {
            NetInfo.Node[] ret = new NetInfo.Node[nodeArray.Length+1];
            for(int i=0;i< nodeArray.Length; ++i) {
                ret[i] = nodeArray[i];
            }
            ret[nodeArray.Length] = node;
            return ret;
        }

        public static NetInfo.Node[] RemoveNode(NetInfo.Node[] nodeArray, NetInfo.Node node) {
            NetInfo.Node[] ret = new NetInfo.Node[nodeArray.Length - 1];
            int j = 0;
            for (int i = 0; i < nodeArray.Length; ++i) {
                if (node == nodeArray[i])
                    continue;
                ret[j++] = nodeArray[i];
            }
            return ret;
        }
    }
}
