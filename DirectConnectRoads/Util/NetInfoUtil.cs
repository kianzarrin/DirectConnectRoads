using ColossalFramework;
using KianCommons;
using KianCommons.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public static void UpdateAllNodeRenderersFor(NetInfo info) {
            try {
                Log.Called();
                for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID) {
                    ref NetNode node = ref nodeID.ToNode();
                    if (!NetUtil.IsNodeValid(nodeID)) continue;
                    if (node.Info.IsRoad()) continue;
                    if (node.m_flags.IsFlagSet(NetNode.Flags.Junction)) continue;
                    if (node.Info != info) continue;
                    NetManager.instance.UpdateNodeRenderer(nodeID, true);
                }
            } catch (Exception ex) { ex.Log(); }
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

                Log.Info($"UnsupportedRoadWithTrack({info.name}) : connectGroup={info.m_connectGroup} tram dist = {dist}", false);
                if ((info.m_connectGroup & NetInfo.ConnectGroup.LargeWideTram) == 0 && dist > 6.3f) {
                    // TODO: is this necessary?
                    // legacy support. Tram too large without LargeWideTram
                    return true;
                }
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


        struct NormalRoadCacheT {
            public int PedestrianLanes;
            public bool Normal;
        }
        static Dictionary<NetInfo, NormalRoadCacheT> normalRoadCache_ = new(500);
        public static void Reset() => normalRoadCache_.Clear();

        public static bool IsNormalSymetricalTwoWay(this NetInfo info, out int pedestrianLanes) {
            if(normalRoadCache_.TryGetValue(info, out NormalRoadCacheT data)){
                pedestrianLanes = data.PedestrianLanes;
                return data.Normal;
            }

            bool normal = IsNormalSymetricalTwoWayImpl(info, out pedestrianLanes);
            normalRoadCache_[info] = new NormalRoadCacheT {
                Normal = normal,
                PedestrianLanes = pedestrianLanes,
            };
            return normal;
        }

        private static bool IsNormalSymetricalTwoWayImpl(this NetInfo info, out int pedestrianLanes) {
            pedestrianLanes = -1;
            if (info.m_netAI is PedestrianZoneRoadAI) {
                Log.Debug($"{info} has PedestrianZoneRoadAI");
                return false;
            }
            if (info.m_class.name == "Pedestrian Street") {
                Log.Debug($"{info} is Pedestrian Street");
                return false;
            }
            bool ret = info.m_forwardVehicleLaneCount == info.m_backwardVehicleLaneCount && info.m_hasBackwardVehicleLanes;
            if (!ret) {
                Log.Debug($"{info}: {info.m_forwardVehicleLaneCount} {info.m_backwardVehicleLaneCount}");
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
                    if (lane.m_direction == NetInfo.Direction.Forward) {
                        forwardbikeLanes++;
                    } else if (lane.m_direction == NetInfo.Direction.Backward) {
                        backwardbikeLanes++;
                    } else {
                        Log.Info($"unexpected bike lane direction: index=[{Array.IndexOf(info.m_lanes, lane)}] " + lane.m_direction);
                        return false;
                    }
                }
            }
            if (forwardbikeLanes != backwardbikeLanes) {
                Log.Debug($"info: {forwardbikeLanes} {backwardbikeLanes}");
                return false;
            }
            if (parkingLanes % 2 != 0 && parkingLanes != 0) {
                Log.Debug($"info: {parkingLanes} {parkingLanes}");
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
                    else if(voffset >=0 )
                        Log.Info($"Skipping {info} because car lanes offset is too high. voffset={voffset}", false);
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
            offset = info.m_surfaceLevel;
            if (offset >= 0) {
                return false;
            }
            foreach (var lane in info.m_lanes) {
                bool isCarLane = lane.m_vehicleType.IsFlagSet(LaneArrowManager.VehicleTypes) &&
                                 lane.m_laneType.IsFlagSet(LaneArrowManager.LaneTypes);
                if (isCarLane && offset != lane.m_verticalOffset)
                    return false;
            }
            return true;
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

        public static Dictionary<NetInfo,float> OriginalTurnAngles = new ();
        public static void FixMaxTurnAngles() {
            if(!DCRConfig.Config.RemoveDCRestrictionsAngle) {
                Log.Info($"skipping {ThisMethod} because RemoveDCRestrictionsAngle={DCRConfig.Config.RemoveDCRestrictionsAngle}");
                return;
            }

            Log.Called();
            foreach(NetInfo netInfo in IterateRoadPrefabs()) {
                FixMaxTurnAnglesFor(netInfo);
            }
        }

        public static void FixMaxTurnAnglesFor(NetInfo netInfo) {
            if (!DCRConfig.Config.RemoveDCRestrictionsAngle) {
                Log.Info($"skipping {ThisMethod} because RemoveDCRestrictionsAngle={DCRConfig.Config.RemoveDCRestrictionsAngle}");
                return;
            }

            try {
                if (netInfo.m_connectGroup == NetInfo.ConnectGroup.None) return;
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
        }

        public static void RestoreMaxTurnAngles() {
            Log.Called();
            foreach (var pair in OriginalTurnAngles) {
                try {
                    NetInfo info = pair.Key;
                    float angle = pair.Value;
                    info.SetMaxTurnAngle(angle);
                } catch (Exception e) {
                    Log.Error(e.Message);
                }
            }
            OriginalTurnAngles.Clear();
        }

        public static void RestoreMaxTurnAnglesFor(NetInfo info) {
            Log.Called();
            try {
                float angle = OriginalTurnAngles[info];
                info.SetMaxTurnAngle(angle);
            } catch (Exception e) {
                Log.Error(e.Message);
            }
        }
        #endregion

        #region fix flags
        public static Dictionary<NetInfo.Node, NetNode.Flags> OriginalForbiddenFalgs = new ();
        public static void FixDCFlags() {
            Log.Called();
            var excemptions = DCRConfig.Config.ExemptionsSet;
            foreach (NetInfo netInfo in IterateRoadPrefabs()) {
                FixDCFlagsFor(netInfo);
            }
        }
        public static void FixDCFlagsFor(NetInfo netInfo) {
            Log.Called();
            var excemptions = DCRConfig.Config.ExemptionsSet;
            try {
                if (netInfo?.m_netAI == null || netInfo.m_nodes == null) return;
                if (excemptions.Contains(netInfo.name)) return;
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
        }

        public static void RestoreFlags() {
            Log.Called();
            foreach (var pair in OriginalForbiddenFalgs) {
                var nodeInfo = pair.Key;
                var flags = pair.Value;
                nodeInfo.m_flagsForbidden = flags;
            }
            OriginalForbiddenFalgs.Clear();
        }

        public static void RestoreFlagsFor(NetInfo.Node nodeInfo) {
            try {
                Assertion.AssertNotNull(nodeInfo, "item");
                var flags = OriginalForbiddenFalgs[nodeInfo];
                nodeInfo.m_flagsForbidden = flags;
            } catch (Exception e) {
                Log.Error(e.Message);
            }
        }
        #endregion

        public static bool IsExempt(NetInfo netInfo) {
            var excemptions = DCRConfig.Config.ExemptionsSet;
            return excemptions.Contains(netInfo.name);

        }

        public static void Exempt(NetInfo netInfo) {
            try {
                SimulationManager.instance.ForcedSimulationPaused = true;
                var excemptions = DCRConfig.Config.ExemptionsSet;
                excemptions.Add(netInfo.name);
                foreach (var nodeInfo in netInfo.m_nodes) {
                    NetInfoUtil.RestoreFlagsFor(nodeInfo);
                }
                NetInfoUtil.RestoreMaxTurnAnglesFor(netInfo);
            } catch (Exception ex) {
                ex.Log();
            } finally {
                SimulationManager.instance.ForcedSimulationPaused = false;
            }

            SimulationManager.instance.AddAction(delegate () {
                NetInfoUtil.UpdateAllNodeRenderersFor(netInfo);
            });
        }

        public static void UnExempt(NetInfo netInfo) {
            try {
                SimulationManager.instance.ForcedSimulationPaused = true;
                var excemptions = DCRConfig.Config.ExemptionsSet;
                excemptions.Remove(netInfo.name);
                NetInfoUtil.FixMaxTurnAnglesFor(netInfo);
                NetInfoUtil.FixDCFlagsFor(netInfo);
            } catch (Exception ex) {
                ex.Log();
            } finally {
                SimulationManager.instance.ForcedSimulationPaused = false;
            }

            SimulationManager.instance.AddAction(delegate () {
                NetInfoUtil.UpdateAllNodeRenderersFor(netInfo);
            });
        }
    }
}
