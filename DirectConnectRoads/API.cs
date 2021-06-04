namespace DirectConnectRoads {
    using System;
    using System.Reflection;
    using DirectConnectRoads.LifeCycle;
    using System.Collections.Generic;

    public static class API {
        public static Version ModVersion => Mod.ModVersion;
        public static bool IsModEnabled => Mod.IsEnabled;


        static List<Func<bool, NetInfo>> ShouldManageDCNodes_;
        public static event Func<bool, NetInfo> ShouldManageDCNodes {
            add {
                ShouldManageDCNodes_ ??= new List<Func<bool, NetInfo>>();
                ShouldManageDCNodes_.Add(value);
            }
            remove {
                ShouldManageDCNodes_.Remove(value);
            }
        }

        internal static bool InvokeShouldManageDCNodes(NetInfo info) {
            if (ShouldManageDCNodes_ == null) 
                return true;

            for (int i = 0; i < ShouldManageDCNodes_.Count; ++i) {
                bool ret = ShouldManageDCNodes_[i](info);
                if (!ret)
                    return false;
            }

            return true;
        }
    }
}
