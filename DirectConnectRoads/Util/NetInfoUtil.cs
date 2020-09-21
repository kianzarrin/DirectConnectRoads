using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KianCommons;
using UnityEngine;
using ColossalFramework;
using static KianCommons.Math.MathUtil;

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

        public static IEnumerable<NetInfo> IterateRoadPrefabs() {
            int prefabCount = PrefabCollection<NetInfo>.PrefabCount();
            int loadedCount = PrefabCollection<NetInfo>.LoadedCount();
            Log.Info($"IterateRoadPrefabs: prefabCount={prefabCount} LoadedCount={loadedCount}");
            for (uint i = 0; i < loadedCount; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (!info) {
                    Log.Error("Warning:Skipping Bad prefab with null info");
                    continue;
                } else if (info.m_netAI == null) {
                    Log.Error("Warning:Skipping Bad prefab with null info.m_NetAI");
                    continue;
                }
                if (!(info.m_netAI is RoadBaseAI))
                    continue;
                yield return info;
            } // end for
        }

        public static bool HasDCMedian(NetInfo netInfo) {
            foreach (NetInfo.Node nodeInfo in netInfo.m_nodes) {
                bool isDC = nodeInfo.m_directConnect && nodeInfo.m_connectGroup != 0;
                if(isDC && DirectConnectUtil.IsMedian(nodeInfo, netInfo))
                    return true;
            }
            return false;
        }



        public static HashSet<NetInfo> UnsupportedRoadWithTrackTable = new HashSet<NetInfo>();

        /// <summary>
        /// returns true if too many tracks 
        /// or if tracks are too far apart.
        /// </summary>
        public static bool UnsupportedRoadWithTrack(NetInfo info) {
            Log.Debug($"UnsupportedRoadWithTrack({info.name} called", false);
            var trainTracks = new List<NetInfo.Lane>();
            var tramTracks = new List<NetInfo.Lane>();
            var MetroTracks = new List<NetInfo.Lane>();
            var monoTracks = new List<NetInfo.Lane>();
            foreach (var lane in info.m_lanes) {
                if (lane.m_vehicleType.IsFlagSet(VehicleInfo.VehicleType.Train)) {
                    trainTracks.Add(lane);
                } else if (lane.m_vehicleType.IsFlagSet(VehicleInfo.VehicleType.Tram)) {
                    tramTracks.Add(lane);
                } else if (lane.m_vehicleType.IsFlagSet(VehicleInfo.VehicleType.Metro)) {
                    MetroTracks.Add(lane);
                } else if (lane.m_vehicleType.IsFlagSet(VehicleInfo.VehicleType.Monorail)) {
                    monoTracks.Add(lane);
                }
            }

            if (trainTracks.Count > 2 || tramTracks.Count > 2 || MetroTracks.Count > 2 || monoTracks.Count > 2)
                return true;

            if (tramTracks.Count == 2) {
                var dist = Mathf.Abs(tramTracks[0].m_position - tramTracks[1].m_position);
                Log.Debug($"UnsupportedRoadWithTrack({info.name} tram dist = " + dist, false);
                if (dist > 6.3f) return true;
            }

            if (trainTracks.Count == 2) {
                var dist = Mathf.Abs(trainTracks[0].m_position - trainTracks[1].m_position);
                if (!EqualAprox(dist, 4f)) return true;
            }
            if (MetroTracks.Count == 2) {
                var dist = Mathf.Abs(MetroTracks[0].m_position - MetroTracks[1].m_position);
                if (!EqualAprox(dist, 4f)) return true;
            }
            if (monoTracks.Count == 2) {
                var dist = Mathf.Abs(monoTracks[0].m_position - monoTracks[1].m_position);
                if (!EqualAprox(dist, 3f)) return true;
            }

            return false;
        }

        // must be called before FixMaxTurnAngles()
        public static void LoadDCTextures() {
            AddedNodes = new List<NetInfo.Node>(100);
            Log.Info("LoadDCTextures() called");
            foreach (NetInfo info in IterateRoadPrefabs()) {
                if (info == null || info.m_nodes.Length == 0)
                    continue;
                if (UnsupportedRoadWithTrack(info)) {
                    Log.Debug($"UnsupportedRoadWithTrackTable.Add({info})",false);
                    UnsupportedRoadWithTrackTable.Add(info);
                    continue;
                }
                if (HasDCMedian(info))
                    continue;
                //if (info.name != "1847143370.Medium Four Lane Road_Data")
                //    continue; // TODO DELETE
                AddDCTextures(info);
            } // end for
        }

        public static void AddDCTextures(NetInfo netInfo) {
            try {
                var nodes = NodeInfoUtil.CreateDCNodes(netInfo.m_nodes[0], netInfo);
                if (nodes == null) return;
                foreach (var node in nodes) {
                    netInfo.m_nodes = NodeInfoUtil.AddNode(netInfo.m_nodes, node);
                    netInfo.m_connectGroup |= node.m_connectGroup;
                    netInfo.m_nodeConnectGroups |= node.m_connectGroup;
                    netInfo.m_requireDirectRenderers = true;
                    AddedNodes.Add(node);
                }
            }
            catch(Exception e) {
                Log.Error(e.ToString());
            }
        }

        public static List<NetInfo.Node> AddedNodes;

        public static void UnloadDCTextures() {
            foreach (NetInfo info in IterateRoadPrefabs())
                RemoveDCTextures(info);
        }

        public static void RemoveDCTextures(NetInfo netInfo) {
            var node = netInfo.m_nodes[netInfo.m_nodes.Length - 1];
            if (AddedNodes.Contains(node)) {
                netInfo.m_nodes = NodeInfoUtil.RemoveNode(netInfo.m_nodes, node);
            }
        }

#if OLDCODE
        static string[] names_ = new string[] { "Medium Road", "Medium Road Decoration Trees", "Medium Road Decoration Grass" };
        public static void ManualLoad() {
            NetInfo sourceInfo = GetInfo("1319965985.4-Lane Road with Junction Median_Data");
            if (sourceInfo == null) return;
            foreach (var name in names_) {
                var targetInfo = GetInfo(name);
                if (targetInfo.m_nodes.Length == 1) {
                    targetInfo.m_nodes = new[] { targetInfo.m_nodes[0], sourceInfo.m_nodes[1] };
                }
                targetInfo.m_connectGroup = sourceInfo.m_connectGroup;
                targetInfo.m_nodeConnectGroups = sourceInfo.m_nodeConnectGroups;
                targetInfo.m_requireDirectRenderers = true;
            }
        }

        public static void ManualUnLoad() {
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
#endif
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
                        Log.Error("Warning:Bad prefab with null info");
                        continue;
                    } else if (netInfo.m_netAI == null) {
                        Log.Error("Warning:Bad prefab with null info.m_NetAI");
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

        #region fix flags
        public static Hashtable OriginalForbiddenFalgs = new Hashtable();
        public static void FixDCFlags() {
            int loadedCount = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < loadedCount; ++i) {
                try {
                    NetInfo netInfo = PrefabCollection<NetInfo>.GetLoaded(i);
                    if (netInfo?.m_netAI == null || netInfo.m_nodes == null) continue;
                    foreach (var nodeInfo in netInfo.m_nodes) {
                        if (!nodeInfo.m_directConnect) continue;
                        bool isMedian = DirectConnectUtil.IsMedian(nodeInfo: nodeInfo, netInfo: netInfo);
                        if (!isMedian) continue;

                        var flags = nodeInfo.m_flagsForbidden & ~(NetNode.Flags.Transition | NetNode.Flags.TrafficLights);
                        if(nodeInfo.m_flagsForbidden != flags) {
                            OriginalForbiddenFalgs[netInfo] = nodeInfo.m_flagsForbidden;
                            nodeInfo.m_flagsForbidden = flags;
                        }
                    }
                }
                catch (Exception e) {
                    Log.Error(e.ToString());
                }
            } // end for            
        }

        public static void RestoreFlags() {
            foreach (NetInfo.Node nodeInfo in OriginalForbiddenFalgs.Keys) {
                try {
                    Assertion.AssertNotNull(nodeInfo, "item");
                    var flags = (NetNode.Flags)OriginalForbiddenFalgs[nodeInfo];
                    nodeInfo.m_flagsForbidden = flags;
                } catch (Exception e) {
                    Log.Error(e.Message);
                }
            }
            OriginalForbiddenFalgs.Clear();
        }
        #endregion

    }
}
