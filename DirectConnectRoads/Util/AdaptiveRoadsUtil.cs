
namespace DirectConnectRoads.Util {
    using System;
    using KianCommons;
    using static KianCommons.PluginUtil;
    using static ColossalFramework.Plugins.PluginManager;
    using System.Reflection;

    internal static class AdaptiveRoadsUtil {
        static PluginInfo plugin => GetAdaptiveRoads();
        public static Assembly asm => plugin.GetMainAssembly();
        public static bool IsActive => plugin.IsActive();
        public static MethodInfo mIsAdaptive =>
            asm.GetType("AdaptiveRoads.Manager.NetInfoExt", throwOnError: true, ignoreCase: true)
            .GetMethod("IsAdaptive") ?? throw new Exception("IsAdaptive not found");

        public static bool IsAdaptive(this NetInfo info) {
            if (!IsActive)
                return false;
            var arg = new object[] { info };
            return (bool)mIsAdaptive.Invoke(null, arg);
        }
    }
}
