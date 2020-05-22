using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace DirectConnectRoads.Util {
    public static class NetInfoUtil {

        #region Textures
        static string name = "Medium Road Decoration Trees" + "4-Lane Road with Junction Median_Data";

        public static NetInfo GetInfo(string name) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.name == name)
                    return info;
                //Helpers.Log(info.name);
            }
            throw new Exception("NetInfo not found!");
        }

        static string[] names = new string[] { "Medium Road", "Medium Road Decoration Trees", "Medium Road Decoration Grass" };
        public static void LoadDCTextures() {
            NetInfo sourceInfo = GetInfo("1319965985.4-Lane Road with Junction Median_Data");
            foreach (var name in names) {
                var targetInfo = GetInfo(name);
                if(targetInfo.m_nodes.Length==1) {
                    targetInfo.m_nodes = new[] { targetInfo.m_nodes[0], sourceInfo.m_nodes[1] };
                }
            }


        }

        public static void UnloadDCTextures() {
            foreach (var name in names) {
                var info = GetInfo(name);
                if (info.m_nodes.Length >= 2) {
                    info.m_nodes = new[] { info.m_nodes[0]};
                }
            }
        }
        #endregion



        #region MaxTurnAngle
        public static void SetMaxTurnAngle(this NetInfo info, float angle) {
            info.m_maxTurnAngle = angle;
            info.m_maxTurnAngleCos = Mathf.Acos( info.m_maxTurnAngle);
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
                    if (hasTracks) {
                        if (!OriginalTurnAngles.ContainsKey(netInfo))
                            OriginalTurnAngles[netInfo] = netInfo.m_maxTurnAngle;
                        netInfo.SetMaxTurnAngle(179);
                    }
                }
                catch (Exception e) {
                    Log.Error(e.ToString());
                }
            } // end for            
        }

        public static void RestoreMaxTurnAngles() {
            foreach (NetInfo info in OriginalTurnAngles) {
                if (info == null) {
                    Log.Error("info==null");
                    continue;
                }
                info.SetMaxTurnAngle((float)OriginalTurnAngles[info]);
            }
            OriginalTurnAngles.Clear();
        }
        #endregion



    }
}
