namespace DirectConnectRoads.Util {
    using ColossalFramework;
    using CSUtil.Commons;
    using KianCommons.Math;
    using System.Collections.Generic;
    using System.Collections;
    using TrafficManager.API.Traffic.Enums;
    using TrafficManager.Manager.Impl;
    using UnityEngine;
    using KianCommons;
    using VectorUtil = KianCommons.Math.VectorUtil;
    using System;

    public static class DirectConnectUtil {
        public unsafe struct FastSegmentList : IEnumerable<ushort>{
            const int MAX_SIZE = 8;
            int size_;
            fixed ushort segments_[MAX_SIZE];

            public int Count => size_;

            public void Add(ushort value) {
                if (size_ >= MAX_SIZE)
                    throw new Exception("List grows too big (max size is 10)");
                segments_[size_++] = value;
            }

            public ushort this[int index] {
                get {
                    if (index < size_)
                        return segments_[index];
                    else
                        throw new IndexOutOfRangeException($"index:{index} size:{size_}");
                }
                set {
                    if (index < size_)
                        segments_[index] = value;
                    else
                        throw new IndexOutOfRangeException($"index:{index} size:{size_}");
                }
            }

            public void Clear() => size_ = 0;

            #region iterator
            IEnumerator<ushort> IEnumerable<ushort>.GetEnumerator() => new Iterator(this);
            IEnumerator IEnumerable.GetEnumerator() => new Iterator(this);

            public struct Iterator : IEnumerator<ushort> {
                int i_;
                FastSegmentList list_;
                ushort current_;

                public Iterator(FastSegmentList list) {
                    i_ = 0;
                    list_ = list;
                    current_ = 0;
                }

                public bool MoveNext() {
                    if (i_ < list_.size_) {
                        current_ = list_[i_++];
                        return true;
                    } else {
                        return false;
                    }
                }

                public void Reset() => i_ = current_ = 0;

                public ushort Current => current_;
                public Iterator GetEnumerator() => this;
                public void Dispose() => Reset();
                object IEnumerator.Current => Current;
            }
            #endregion
        }


        #region Median Texture Detection
        public static bool IsMedian(this NetInfo.Node nodeInfo, NetInfo netInfo) {
            var nodeInfoVehicleTypes = GetVehicleType(nodeInfo.m_connectGroup);
            return (netInfo.m_vehicleTypes & nodeInfoVehicleTypes) == VehicleInfo.VehicleType.None;
        }

        //public const VehicleInfo.VehicleType TRACK_VEHICLE_TYPES =
        //    VehicleInfo.VehicleType.Tram |
        //    VehicleInfo.VehicleType.Metro |
        //    VehicleInfo.VehicleType.Train |
        //    VehicleInfo.VehicleType.Monorail;
        internal static VehicleInfo.VehicleType GetVehicleType(NetInfo.ConnectGroup flags) {
            VehicleInfo.VehicleType ret = 0;
            const NetInfo.ConnectGroup TRAM =
                NetInfo.ConnectGroup.CenterTram |
                NetInfo.ConnectGroup.NarrowTram |
                NetInfo.ConnectGroup.SingleTram |
                NetInfo.ConnectGroup.WideTram;
            const NetInfo.ConnectGroup TRAIN =
                NetInfo.ConnectGroup.DoubleTrain |
                NetInfo.ConnectGroup.SingleTrain |
                NetInfo.ConnectGroup.TrainStation;
            const NetInfo.ConnectGroup MONO_RAIL =
                NetInfo.ConnectGroup.DoubleMonorail |
                NetInfo.ConnectGroup.SingleMonorail |
                NetInfo.ConnectGroup.MonorailStation;
            const NetInfo.ConnectGroup METRO =
                NetInfo.ConnectGroup.DoubleMetro |
                NetInfo.ConnectGroup.SingleMetro |
                NetInfo.ConnectGroup.MetroStation;
            const NetInfo.ConnectGroup TROLLY =
                NetInfo.ConnectGroup.CenterTrolleybus |
                NetInfo.ConnectGroup.NarrowTrolleybus |
                NetInfo.ConnectGroup.SingleTrolleybus |
                NetInfo.ConnectGroup.WideTrolleybus;

            if ((flags & TRAM) != 0) {
                ret |= VehicleInfo.VehicleType.Tram;
                ret |= VehicleInfo.VehicleType.Metro; // MOM
            }
            if ((flags & METRO) != 0) {
                ret |= VehicleInfo.VehicleType.Metro;
            }
            if ((flags & TRAIN) != 0) {
                ret |= VehicleInfo.VehicleType.Train;
            }
            if ((flags & MONO_RAIL) != 0) {
                ret |= VehicleInfo.VehicleType.Monorail;
            }
            if ((flags & TROLLY) != 0) {
                ret |= VehicleInfo.VehicleType.Trolleybus;
            }
            return ret;
        }
        #endregion

        #region Broken Median detection
        public static bool OpenMedian(ushort segmentID1, ushort segmentID2) {
            ushort nodeID = segmentID1.ToSegment().GetSharedNode(segmentID2);
            bool connected = DoesSegmentGoToSegment(segmentID1, segmentID2, nodeID);
            connected |= DoesSegmentGoToSegment(segmentID2, segmentID1, nodeID);
            if (!connected)
                return true;

            return IsMedianBrokenHelper(segmentID1, segmentID2) ||
                   IsMedianBrokenHelper(segmentID2, segmentID1);
        }

        static bool IntersectAny(FastSegmentList segmentsA, FastSegmentList segmentsB) {
            for (int a = 0; a < segmentsA.Count; ++a)
                for (int b = 0; b < segmentsB.Count; ++b)
                    if (segmentsA[a] == segmentsB[b])
                        return true;
            return false;
        }

        public static bool IsMedianBrokenHelper(ushort segmentID, ushort otherSegmentID) {
            ushort nodeID = segmentID.ToSegment().GetSharedNode(otherSegmentID);

            GetGeometry(segmentID, otherSegmentID, out var leftSegments, out var rightSegments);
            leftSegments.Add(segmentID); // non of the left segments shall go to current segment.
            rightSegments.Add(segmentID); // current segment shall not go to any of the left segments(including iteself)
            //Log.Debug($"IsMedianBrokenHelper({segmentID} ,{otherSegmentID}) :\n" +
            //    $"leftSegments={leftSegments.ToSTR()} , rightSegments={rightSegments.ToSTR()}");

            foreach (ushort rightSegmentID in rightSegments) {
                var targetSegments = GetTargetSegments(rightSegmentID, nodeID);
                if (IntersectAny(targetSegments, leftSegments)) {
                    //Log.Debug($"intersection detected rightSegmentID:{rightSegmentID} targetSegments:{targetSegments.ToSTR()} leftSegments:{leftSegments.ToSTR()}");
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region segment 2 segment connections
        /// <summary>
        /// returns a list of all segments sourceSegmentID is connected to including itself
        /// if uturn is allowed.
        /// </summary>
        /// <param name="sourceSegmentID"></param>
        /// <param name="nodeID"></param>
        /// <returns></returns>
        public static FastSegmentList GetTargetSegments(ushort sourceSegmentID, ushort nodeID) {
            var ret = new FastSegmentList();
            foreach (ushort targetSegmentID in NetUtil.IterateNodeSegments(nodeID)) {
                if (DoesSegmentGoToSegment(sourceSegmentID, targetSegmentID, nodeID))
                    ret.Add(targetSegmentID);
            }
            return ret;
        }

        /// <summary>
        /// Determines if any lane from source segment goes to the target segment
        /// based on lane arrows and lane connections.
        public static bool DoesSegmentGoToSegment(ushort sourceSegmentID, ushort targetSegmentID, ushort nodeID) {
            bool startNode = NetUtil.IsStartNode(sourceSegmentID, nodeID);
            if (sourceSegmentID == targetSegmentID) {
                return JunctionRestrictionsManager.Instance.IsUturnAllowed(sourceSegmentID, startNode);
            }
            ArrowDirection arrowDir = ExtSegmentEndManager.Instance.GetDirection(sourceSegmentID, targetSegmentID, nodeID);
            LaneArrows arrow = ArrowDir2LaneArrows(arrowDir);
            var sourceLanes = NetUtil.IterateLanes(
                sourceSegmentID,
                startNode,
                LaneArrowManager.LANE_TYPES,
                LaneArrowManager.VEHICLE_TYPES);
            foreach (LaneData sourceLane in sourceLanes) {
                bool connected;
                if (LaneConnectionManager.Instance.HasConnections(sourceLane.LaneID, startNode)) {
                    connected = IsLaneConnectedToSegment(sourceLane.LaneID, targetSegmentID);
                    //Log.Debug($"IsLaneConnectedToSegment({sourceLane},{targetSegmentID}) = {connected}");

                } else {
                    LaneArrows arrows = LaneArrowManager.Instance.GetFinalLaneArrows(sourceLane.LaneID);
                    connected = arrows.IsFlagSet(arrow);
                }
                if (connected)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if there is any lane connection from source lane to target segment.
        /// </summary>
        public static bool IsLaneConnectedToSegment(uint sourceLaneId, ushort targetSegmentID) {
            ushort sourceSegmentID = sourceLaneId.ToLane().m_segment;
            ushort nodeID = sourceSegmentID.ToSegment().GetSharedNode(targetSegmentID);
            bool sourceStartNode = NetUtil.IsStartNode(sourceSegmentID, nodeID);
            bool targetStartNode = NetUtil.IsStartNode(targetSegmentID, nodeID);


            var targetLanes = new LaneDataIterator(
                targetSegmentID,
                !targetStartNode,// going away from start node.
                LaneConnectionManager.LANE_TYPES,
                LaneConnectionManager.VEHICLE_TYPES);
            foreach (LaneData targetLane in targetLanes) {
                if (LaneConnectionManager.Instance.AreLanesConnected(sourceLaneId, targetLane.LaneID, sourceStartNode))
                    return true;
            }
            return false;
        }

        public static LaneArrows ArrowDir2LaneArrows(ArrowDirection arrowDir) {
            switch (arrowDir) {
                case ArrowDirection.Forward:
                    return LaneArrows.Forward;
                case ArrowDirection.Left:
                    return LaneArrows.Left;
                case ArrowDirection.Right:
                    return LaneArrows.Right;
                default:
                    return LaneArrows.None;
            }
        }
        #endregion

        #region Geometry

        public static void GetGeometry(ushort segmentID1, ushort segmentID2,
            out FastSegmentList leftSegments, out FastSegmentList rightSegments) {
            leftSegments = new FastSegmentList();
            rightSegments = new FastSegmentList();
            ushort nodeID = segmentID1.ToSegment().GetSharedNode(segmentID2);
            var angle0 = GetSegmentsAngle(segmentID1, segmentID2);
            for (int i = 0; i < 8; ++i) {
                ushort segmentID = nodeID.ToNode().GetSegment(i);
                if (segmentID == 0 || segmentID == segmentID1 || segmentID == segmentID2)
                    continue;
                var angle = GetSegmentsAngle(segmentID1, segmentID);
                bool right = VectorUtil.CompareAngles_CCW_Right(source: angle0, target: angle);
                //Log.Debug($"GetGeometry({segmentID1}, {segmentID2}) : segment:{segmentID}\n" +
                //    $" CompareAngles_CCW_Right(angle0:{angle0*Mathf.Rad2Deg}, angle:{angle*Mathf.Rad2Deg}) -> {right}");
                if (right)
                    rightSegments.Add(segmentID);
                else
                    leftSegments.Add(segmentID);
            }

        }

        public static float GetSegmentsAngle(ushort from, ushort to) {
            ushort nodeID = from.ToSegment().GetSharedNode(to);
            Vector3 dir1 = from.ToSegment().GetDirection(nodeID);
            Vector3 dir2 = to.ToSegment().GetDirection(nodeID);
            float ret = VectorUtil.SignedAngleRadCCW(dir1.ToCS2D(), dir2.ToCS2D());
            //Log.Debug($"SignedAngleRadCCW({dir1} , {dir2}) => {ret}\n"+
            //           $"GetSegmentsAngle({from} , {to}) => {ret}");
            return ret;
        }
        #endregion
    }
}
