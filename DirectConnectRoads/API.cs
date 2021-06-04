namespace DirectConnectRoads {
    using System;
    using System.Reflection;
    using DirectConnectRoads.LifeCycle;
    using System.Collections.Generic;

    public static class API {
        public static Version ModVersion => Mod.ModVersion;
        public static bool IsModEnabled => Mod.IsEnabled;

        /// <summary>
        /// maybe be called without sourceSegmentID and targetSegmentID to decided whether or not to create DC meshes.
        /// </summary>
        public delegate bool ShouldManageDCNodesHandler(NetInfo info, ushort sourceSegmentID, ushort targetSegmentID);
        
        static List<ShouldManageDCNodesHandler> ShouldManageDCNodes_;
        public static event ShouldManageDCNodesHandler ShouldManageDCNodes {
            add {
                ShouldManageDCNodes_ ??= new List<ShouldManageDCNodesHandler>();
                ShouldManageDCNodes_.Add(value);
            }
            remove {
                ShouldManageDCNodes_.Remove(value);
            }
        }

        internal static bool InvokeShouldManageDCNodes(NetInfo info, ushort sourceSegmentID, ushort targetSegmentID) {
            if (ShouldManageDCNodes_ == null) 
                return true;

            for (int i = 0; i < ShouldManageDCNodes_.Count; ++i) {
                bool ret = ShouldManageDCNodes_[i](info, sourceSegmentID, targetSegmentID);
                if (!ret)
                    return false;
            }

            return true;
        }
    }
}
