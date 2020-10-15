namespace DirectConnectRoads {
    using System;
    using System.Runtime.CompilerServices;
    using DirectConnectRoads.LifeCycle;
    
    public static class API {
        public static Version ModVersion => Mod.ModVersion;
        public static bool IsModEnabled => Mod.IsEnabled;

        public static event Func<bool, NetInfo> ShouldCreateDCNodes;
        public static event Func<bool, NetInfo> ShouldManageDCNodes;

        public delegate void NodeClonedHandler(NetInfo.Node target, NetInfo.Node source);
        public static event NodeClonedHandler OnNodeCloned;

        public static event Action<NetInfo> OnNetInfoChanged;

        /* Inokations ***************************/

        internal static bool InvokeShouldCreateDCNodes(NetInfo info) {
            var arg = new object[] { info };
            foreach (var m in ShouldCreateDCNodes.GetInvocationList()) {
                bool ret = (bool)m.DynamicInvoke(arg);
                if (!ret)
                    return false;
            }
            return true;            
        }

        internal static bool InvokeShouldManageDCNodes(NetInfo info) {
            var arg = new object[] { info };
            foreach (var m in ShouldManageDCNodes.GetInvocationList()) {
                bool ret = (bool)m.DynamicInvoke(arg);
                if (!ret)
                    return false;
            }
            return true;
        }

        internal static void InvokeOnNodeCloned(NetInfo.Node target, NetInfo.Node source)
            => OnNodeCloned(target: target, source: source);

        internal static void InvokeOnNetInfoChanged(NetInfo info)
            => OnNetInfoChanged(info);
    }
}
