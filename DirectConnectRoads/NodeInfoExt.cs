using System;
using UnityEngine;

// TODO handle multiple junction nodes.
namespace DirectConnectRoads {
    using HideUnconnectedTracks.Utils;
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
            NetInfo.Node node = Copy(template);
            node.m_nodeMaterial = MaterialUtils.ContinuesMedian(node.m_nodeMaterial, netInfo);
            Mesh segmentMesh = MaterialUtils.ContinuesMedian(node.m_mesh, netInfo);
            node.m_mesh = segmentMesh.CutOutRoadSides();
            if (node.m_mesh == null || node.m_material == null)
                return null;
            node.m_directConnect = true;
            node.m_connectGroup = NetInfo.ConnectGroup.DoubleTrain;
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
