using System;
using UnityEngine;

// TODO handle multiple junction nodes.
namespace DirectConnectRoads {
    using DirectConnectRoads.Utils;
    using System.Linq;
    using Util;

    // [Serializable]
    public static class NodeInfoUtil{
        /// <summary>
        /// creates new clone of the node.
        /// </summary>
        public static NetInfo.Node Copy(NetInfo.Node template) {
            NetInfo.Node node = new NetInfo.Node();
            Extensions.CopyProperties<NetInfo.Node>(node, template);
            return node;
        }

        public static NetInfo.Node CreateDCNode(NetInfo.Node template, NetInfo netInfo) {
            Log.Debug("CreateDCNode called for " + netInfo?.name);
            var material = MaterialUtils.ContinuesMedianMaterial(netInfo);
            var mesh = MaterialUtils.ContinuesMedianMesh(netInfo);
            mesh = mesh?.CutOutRoadSides();
            mesh?.Elevate();
            if (mesh == null || material == null) return null;

            mesh.name += "_DC";
            material.name += "_DC";
            NetInfo.Node node = Copy(template);
            node.m_mesh = node.m_nodeMesh = mesh;
            node.m_material = node.m_nodeMaterial = material;
            node.m_directConnect = true;
            node.m_connectGroup = NetInfo.ConnectGroup.DoubleTrain;

            Log.Debug("CreateDCNode sucessful for " + netInfo?.name);
            node.m_nodeMesh.DumpMesh($"DC mesh for {netInfo.name}");
            return node;
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
