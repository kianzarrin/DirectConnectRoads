using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace DirectConnectRoads.Util {
    public static class NetInfoUtil {
        #region Textures
        public static NetInfo GetInfo(string name) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.name == name)
                    return info;
                //Helpers.Log(info.name);
            }
            Log.Error("NetInfo not found!");
            return null;
        }

        static string[] names_ = new string[] { "Medium Road", "Medium Road Decoration Trees", "Medium Road Decoration Grass" };

        // must be called before FixMaxTurnAngles()
        public static void LoadDCTextures() {
            NetInfo sourceInfo = GetInfo("1319965985.4-Lane Road with Junction Median_Data");
            if (sourceInfo == null) return;
            foreach (var name in names_) {
                var targetInfo = GetInfo(name);
                if(targetInfo.m_nodes.Length==1) {
                    targetInfo.m_nodes = new[] { targetInfo.m_nodes[0], sourceInfo.m_nodes[1] };
                }
                targetInfo.m_connectGroup = sourceInfo.m_connectGroup;
                targetInfo.m_nodeConnectGroups = sourceInfo.m_nodeConnectGroups;
                targetInfo.m_requireDirectRenderers = true;
            }
        }

        public static void UnloadDCTextures() {
            foreach (var name in names_) {
                var info = GetInfo(name);
                if (info == null)
                    continue;
                if (info.m_nodes.Length >= 2) {
                    info.m_nodes = new[] { info.m_nodes[0]};
                }
                info.m_connectGroup = NetInfo.ConnectGroup.None;
                info.m_nodeConnectGroups = NetInfo.ConnectGroup.None;
                info.m_requireDirectRenderers = false;
            }
        }
        #endregion

        #region MaxTurnAngle
        public static void SetMaxTurnAngle(this NetInfo info, float angle) {
            info.m_maxTurnAngle = angle;
            info.m_maxTurnAngleCos = Mathf.Cos(info.m_maxTurnAngle * Mathf.Deg2Rad);
        }

        public static Hashtable OriginalTurnAngles = new Hashtable();
        public static void FixMaxTurnAngles() {
            int loadedCount = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < loadedCount; ++i) {
                try {
                    NetInfo netInfo = PrefabCollection<NetInfo>.GetLoaded(i);
                    if (netInfo == null) {
                        Log.Warning("Bad prefab with null info");
                        continue;
                    } else if (netInfo.m_netAI == null) {
                        Log.Warning("Bad prefab with null info.m_NetAI");
                        continue;
                    }
                    if (netInfo.m_connectGroup == NetInfo.ConnectGroup.None)
                        continue;
                    bool hasTracks = false;
                    foreach (var nodeInfo in netInfo.m_nodes) {
                        bool isMedian = DirectConnectUtil.IsMedian(nodeInfo: nodeInfo, netInfo: netInfo);
                        hasTracks = nodeInfo.m_directConnect && !isMedian;
                    }
                    if (!hasTracks) {
                        if (!OriginalTurnAngles.ContainsKey(netInfo))
                            OriginalTurnAngles[netInfo] = netInfo.m_maxTurnAngle;
                        netInfo.SetMaxTurnAngle(180);
                    }
                }
                catch (Exception e) {
                    Log.Error(e.ToString());
                }
            } // end for            
        }

        public static void RestoreMaxTurnAngles() {
            foreach (var item in OriginalTurnAngles.Keys) {
                NetInfo info = item as NetInfo;
                if (info == null) {
                    Log.Error("info==null item="+item);
                    continue;
                }
                try {
                    float angle = (float)OriginalTurnAngles[info];
                    info.SetMaxTurnAngle(angle);
                }
                catch (Exception e){
                    Log.Error(e.Message);
                }
            }
            OriginalTurnAngles.Clear();
        }
        #endregion
    }
}
