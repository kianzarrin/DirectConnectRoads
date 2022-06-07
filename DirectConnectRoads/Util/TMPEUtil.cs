namespace DirectConnectRoads.Util {
    using KianCommons;
    using TrafficManager.API.Manager;

    public static class TMPEUtil {
        public static IManagerFactory TMPE => TrafficManager.API.Implementations.ManagerFactory;
        public static IJunctionRestrictionsManager JRMan => TMPE?.JunctionRestrictionsManager;
        public static IRoutingManager RMan => TMPE?.RoutingManager;

        public const NetInfo.LaneType LANE_TYPES = NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle;

        public const VehicleInfo.VehicleType VEHICLE_TYPES =
            VehicleInfo.VehicleType.Car
            | VehicleInfo.VehicleType.Train
            | VehicleInfo.VehicleType.Tram
            | VehicleInfo.VehicleType.Metro
            | VehicleInfo.VehicleType.Monorail
            | VehicleInfo.VehicleType.Trolleybus;

        public static LaneTransitionData[] GetForwardRoutings(uint laneID, bool startNode) {
            uint routingIndex = RMan.GetLaneEndRoutingIndex(laneID, startNode);
            return RMan.LaneEndForwardRoutings[routingIndex].transitions;
        }

        public static bool HasCrossing(ushort segmentId, ushort nodeId) {
            bool startNode = segmentId.ToSegment().IsStartNode(nodeId);
            return JRMan.IsPedestrianCrossingAllowed(segmentId, startNode);
        }
    }
}
