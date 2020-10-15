
namespace DirectConnectRoads.Util {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using KianCommons;
    using static KianCommons.PluginUtil;
    using ColossalFramework.Plugins;
    using static ColossalFramework.Plugins.PluginManager;
    using System.Reflection;

    internal static class AdaptiveRoadsUtil {
        static PluginInfo plugin => GetAdaptiveRoads();
        public static Assembly asm => plugin.MainAssembly();
        public static bool IsActive => plugin.IsActive();
        public static bool IsAdaptive(this NetInfo info) {
            if (!IsActive)
                return false;
            Type t = asm.GetType("AdaptiveRoads.Manager.NetInfoExt");
            var method = t.GetMethod("IsAdaptive");
            var arg = new object[] { info };
            return (bool)method.Invoke(null, arg);
        }
    }
}
