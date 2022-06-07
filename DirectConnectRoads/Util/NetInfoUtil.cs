using ColossalFramework;
using KianCommons;
using KianCommons.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using TrafficManager.API.Manager;
using UnityEngine;
using static KianCommons.Math.MathUtil;
using static KianCommons.ReflectionHelpers;

namespace DirectConnectRoads.Util {
    public static class NetInfoUtil {
        static IManagerFactory TMPE => TrafficManager.API.Implementations.ManagerFactory;
        static ILaneArrowManager LaneArrowManager => TMPE.LaneArrowManager;

        //public const float ASPHALT_HEIGHT = RoadMeshUtil.ASPHALT_HEIGHT;
        [Obsolete]
        public static void UpdateAllNodes() {
            Log.Called();
            for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID) {
                if (!NetUtil.IsNodeValid(nodeID)) continue;
                if (!nodeID.ToNode().Info.m_requireDirectRenderers) continue;
                if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction)) continue;
                NetManager.instance.UpdateNodeRenderer(nodeID, true);
            }
        }

        public static void UpdateAllNetworkRenderers() {
            Log.Called();
            for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID) {
                if (!NetUtil.IsNodeValid(nodeID)) continue;
                if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction)) continue;
                NetManager.instance.UpdateNodeRenderer(nodeID, true);
                foreach (ushort segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                    NetManager.instance.UpdateSegmentRenderer(segmentID, true);
                }
            }
        }

        public static void FastUpdateAllRoadJunctions() {
            Log.Called();
            for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID) {
                if (!NetUtil.IsNodeValid(nodeID)) continue;
                if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction)) continue;
                if (!nodeID.ToNode().Info.IsRoad()) continue;

                nodeID.ToNode().UpdateNode(nodeID);
                NetManager.instance.UpdateNodeFlags(nodeID);
                NetManager.instance.UpdateNodeRenderer(nodeID, true);

                foreach (ushort segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                    segmentID.ToSegment().UpdateSegment(segmentID);
                    NetManager.instance.UpdateSegmentFlags(segmentID);
                    NetManager.instance.UpdateSegmentRenderer(segmentID, true);

                }
            }
        }

        public static void FullUpdateAllRoadJunctions() {
            Log.Called();
            for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID) {
                if (!NetUtil.IsNodeValid(nodeID)) continue;
                if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction)) continue;
                if (!nodeID.ToNode().Info.IsRoad()) continue;
                NetManager.instance.UpdateNode(nodeID);
            }
        }

        public static void UpdateAllNodeRenderers() {
            try {
                Log.Called();
                for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID) {
                    if (!NetUtil.IsNodeValid(nodeID)) continue;
                    if (!nodeID.ToNode().Info.IsRoad()) continue;
                    if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction)) continue;
                    NetManager.instance.UpdateNodeRenderer(nodeID, true);
                }
            } catch(Exception ex) { ex.Log(); }
        }


        #region Textures
        public static NetInfo GetInfo(string name) {
            var ret = PrefabCollection<NetInfo>.FindLoaded(name);
            if (ret is null)
                Log.Warning($"NetInfo '{name}' not found!");
            return ret;
        }

        public static IEnumerable<NetInfo> IterateRoadPrefabs() {
            int prefabCount = PrefabCollection<NetInfo>.PrefabCount();
            int loadedCount = PrefabCollection<NetInfo>.LoadedCount();
            //Log.Debug($"IterateRoadPrefabs: prefabCount={prefabCount} LoadedCount={loadedCount}",false);
            for (uint i = 0; i < loadedCount; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (!info) {
                    Log.Warning("Skipping Bad prefab with null info");
                    continue;
                } else if (info.m_netAI == null) {
                    Log.Warning("Skipping Bad prefab with null info.m_NetAI");
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
                if (isDC && DCUtil.IsMedian(nodeInfo, netInfo))
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
            if (info.GetIsAdaptive())
                return false; // handled else where.

            //Log.Debug($"UnsupportedRoadWithTrack({info.name}) called", false);
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

                Log.Info($"UnsupportedRoadWithTrack({info.name}) : tram dist = {dist}", false);
                if (dist > 6.3f) 
                    return true;
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


        public static bool IsRoad(this NetInfo info) => info.m_netAI is RoadBaseAI;

        public static bool IsNormalSymetricalTwoWay(this NetInfo info, out int pedestrianLanes) {
            bool ret = info.m_forwardVehicleLaneCount == info.m_backwardVehicleLaneCount && info.m_hasBackwardVehicleLanes;
            pedestrianLanes = -1;
            if (!ret) {
                //Log.Debug($"info: {info.m_forwardVehicleLaneCount} {info.m_backwardVehicleLaneCount}");
                return false;
            }

            int forwardbikeLanes = 0;
            int backwardbikeLanes = 0;
            int parkingLanes = 0;
            pedestrianLanes = 0;
            foreach (var lane in info.m_lanes) {
                if (lane.m_laneType == NetInfo.LaneType.Pedestrian) {
                    pedestrianLanes++;
                } else if (lane.m_laneType == NetInfo.LaneType.Parking) {
                    parkingLanes++;
                } else if (lane.m_vehicleType.IsFlagSet(VehicleInfo.VehicleType.Bicycle)) {
                    if (lane.m_direction == NetInfo.Direction.Forward)
                        forwardbikeLanes++;
                    if (lane.m_direction == NetInfo.Direction.Backward)
                        backwardbikeLanes++;
                    else
                        return false;
                }
            }
            if (forwardbikeLanes != backwardbikeLanes) {
                //Log.Debug($"info: {forwardbikeLanes} {backwardbikeLanes}");
                return false;
            }
            if (parkingLanes % 2 != 0 && parkingLanes != 0) {
                //Log.Debug($"info: {parkingLanes} {parkingLanes}");
                return false;
            }
            return true;
        }

        // must be called before FixMaxTurnAngles()
        public static void GenerateDCTextures() {
            AddedNodes = new HashSet<NetInfo.Node>();
            if (!DCRConfig.Config.GenerateMedians) {
                Log.Info($"skipping {ThisMethod} because GenerateMedians={DCRConfig.Config.GenerateMedians}");
                return;
            }

            Log.Called();
            foreach (NetInfo info in IterateRoadPrefabs()) {
                if (info == null || info.m_nodes.Length == 0)
                    continue;
                if (!info.IsRoad())
                    continue;
                if (!info.IsNormalSymetricalTwoWay(out _)) {
                    Log.Info($"Skipping {info} because it is !IsNormalSymetricalTwoWay()", false);
                    continue;
                }

                if (!info.m_hasPedestrianLanes) {
                    Log.Info($"Skipping {info} because it has no pedestrian lanes", false);
                    continue;
                }

                if (UnsupportedRoadWithTrack(info)) {
                    Log.Info($"UnsupportedRoadWithTrackTable.Add({info})", false);
                    UnsupportedRoadWithTrackTable.Add(info);
                    continue;
                }
                if (HasDCMedian(info)) {
                    Log.Info($"Skipping {info} because it already has median", false);
                    continue;
                }

                if (!GetAshphaltOffset(info, out float voffset)) {
                    if (float.IsNaN(voffset))
                        Log.Info($"Skipping {info} because it has no car lanes. voffset={voffset}", false);
                    else
                        Log.Info($"Skipping {info} because it has lanes at different vertical offset. voffset={voffset}", false);
                    continue;
                }

                if (info.GetIsAdaptive()) {
                    Log.Info($"Skipping {info} because it belongs to the adaptive roads mod", false);
                    continue;
                }

                if (!API.InvokeShouldManageDCNodes(info, 0, 0)) {
                    Log.Info($"Skipping {info} because InvokeShouldManageDCNodes() returned false", false);
                    continue;
                }

                if (info.IsCSUR()) {
                    Log.Info($"Skipping {info} because its CSUR", false);
                    continue;
                }

                AddDCTextures(info, voffset);
            } // end for
        }

        /// <summary>
        /// returns true if all car lans have the same level. offset==nan
        /// returns false if car lanes have different vertical offsets or no car lane was found. offset==offset of some lane.
        /// </summary>
        /// <param name="offset">vertical offset of the car the car lanes.</param>
        public static bool GetAshphaltOffset(this NetInfo info, out float offset) {
            offset = float.NaN;
            foreach (var lane in info.m_lanes) {
                bool isCarLane = lane.m_vehicleType.IsFlagSet(LaneArrowManager.VehicleTypes) &&
                                 lane.m_laneType.IsFlagSet(LaneArrowManager.LaneTypes);
                if (!isCarLane)
                    continue;
                if (float.IsNaN(offset))
                    offset = lane.m_verticalOffset;
                else if (offset != lane.m_verticalOffset)
                    return false;
            }
            return !float.IsNaN(offset);
        }

        public static void AddDCTextures(NetInfo netInfo, float voffset/* = ASPHALT_HEIGHT*/) {
            try {
                var nodes = NodeInfoUtil.CreateDCNodes(netInfo.m_nodes[0], netInfo, voffset);
                if (nodes == null) return;
                foreach (var node in nodes) {
                    netInfo.m_nodes = NodeInfoUtil.AddNode(netInfo.m_nodes, node);
                    netInfo.m_connectGroup |= node.m_connectGroup;
                    netInfo.m_nodeConnectGroups |= node.m_connectGroup;
                    netInfo.m_requireDirectRenderers = true;
                    AddedNodes.Add(node);
                }
            } catch (Exception e) {
                Log.Error(e.ToString());
            }
        }

        public static HashSet<NetInfo.Node> AddedNodes;

        public static void UnloadDCTextures() {
            Log.Called();
            foreach (NetInfo info in IterateRoadPrefabs())
                RemoveDCTextures(info);
        }

        public static void RemoveDCTextures(NetInfo netInfo) {
            var node = netInfo.m_nodes[netInfo.m_nodes.Length - 1];
            if (AddedNodes.Contains(node))
                netInfo.m_nodes = NodeInfoUtil.RemoveNode(netInfo.m_nodes, node);
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
            if(!DCRConfig.Config.RemoveDCRestrictionsAngle) {
                Log.Info($"skipping {ThisMethod} because RemoveDCRestrictionsAngle={DCRConfig.Config.RemoveDCRestrictionsAngle}");
                return;
            }

            Log.Called();
            foreach(NetInfo netInfo in IterateRoadPrefabs()) {
                try {
                    if (netInfo.m_connectGroup == NetInfo.ConnectGroup.None)
                        continue;
                    bool hasTracks = false;
                    foreach (var nodeInfo in netInfo.m_nodes) {
                        bool isMedian = DCUtil.IsMedian(nodeInfo: nodeInfo, netInfo: netInfo);
                        hasTracks = nodeInfo.m_directConnect && !isMedian;
                    }
                    if (!hasTracks) {
                        if (!OriginalTurnAngles.ContainsKey(netInfo))
                            OriginalTurnAngles[netInfo] = netInfo.m_maxTurnAngle;
                        netInfo.SetMaxTurnAngle(180);
                    }
                } catch (Exception e) {
                    Log.Error(e.ToString());
                }
            } // end for            
        }

        public static void RestoreMaxTurnAngles() {
            Log.Called();
            foreach (var item in OriginalTurnAngles.Keys) {
                NetInfo info = item as NetInfo;
                if (info == null) {
                    Log.Error("info==null item=" + item);
                    continue;
                }
                try {
                    float angle = (float)OriginalTurnAngles[info];
                    info.SetMaxTurnAngle(angle);
                } catch (Exception e) {
                    Log.Error(e.Message);
                }
            }
            OriginalTurnAngles.Clear();
        }
        #endregion

        #region fix flags
        public static Hashtable OriginalForbiddenFalgs = new Hashtable();
        public static void FixDCFlags() {
            Log.Called();
            foreach (NetInfo netInfo in IterateRoadPrefabs()) {
                try {
                    if (netInfo?.m_netAI == null || netInfo.m_nodes == null) continue;
                    foreach (var nodeInfo in netInfo.m_nodes) {
                        if (!nodeInfo.m_directConnect) continue;
                        bool isMedian = DCUtil.IsMedian(nodeInfo: nodeInfo, netInfo: netInfo);
                        if (!isMedian) continue;

                        var flags = nodeInfo.m_flagsForbidden;
                        if (DCRConfig.Config.RemoveDCRestrictionsTransition)
                            flags &= ~NetNode.Flags.Transition;
                        if (DCRConfig.Config.RemoveDCRestrictionsTL)
                            flags &= ~NetNode.Flags.TrafficLights;
                        if (nodeInfo.m_flagsForbidden != flags) {
                            OriginalForbiddenFalgs[nodeInfo] = nodeInfo.m_flagsForbidden;
                            nodeInfo.m_flagsForbidden = flags;
                        }
                    }
                } catch (Exception e) {
                    Log.Error(e.ToString());
                }
            } // end for            
        }

        public static void RestoreFlags() {
            Log.Called();
            foreach (var key in OriginalForbiddenFalgs.Keys) {
                try {
                    Assertion.AssertNotNull(key is NetInfo.Node, "key is NetInfo.Node");
                    Assertion.Assert(key is NetInfo.Node, "key is NetInfo.Node");
                    NetInfo.Node nodeInfo = key as NetInfo.Node;
                    Assertion.AssertNotNull(nodeInfo, "item");
                    var value = OriginalForbiddenFalgs[nodeInfo];
                    Assertion.Assert(value.GetType() == typeof(NetNode.Flags), $"{value}.type:{value.GetType()}==typeof(NetNode.Flags)");
                    var flags = (NetNode.Flags)value;
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
