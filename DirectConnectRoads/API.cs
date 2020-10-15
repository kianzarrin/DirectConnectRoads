namespace DirectConnectRoads {
    using System;
    using System.Runtime.CompilerServices;
    using DirectConnectRoads.LifeCycle;
    
    public static class API {
        public static Version ModVersion => Mod.ModVersion;
        public static bool IsModEnabled => Mod.IsEnabled;

        public static event Func<bool, NetInfo> ShouldManageDCNodes;

        /* Inokations ***************************/

        internal static bool InvokeShouldManageDCNodes(NetInfo info) {
            var arg = new object[] { info };
            foreach (var m in ShouldManageDCNodes.GetInvocationList()) {
                bool ret = (bool)m.DynamicInvoke(arg);
                if (!ret)
                    return false;
            }
            return true;
        }
    }
}
