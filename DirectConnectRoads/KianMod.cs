using HarmonyLib;
using ICities;
using JetBrains.Annotations;
using DirectConnectRoads.Util;
using CitiesHarmony.API;
using System.Runtime.CompilerServices;
using DirectConnectRoads.Patches;

namespace DirectConnectRoads {
    public class KianModInfo : IUserMod {
        public string Name => "Direct Connect Roads";
        public string Description => "uses Direct Connect textures if TMPE rules suggests unbroken median";

        [UsedImplicitly]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnEnabled() {
            System.IO.File.WriteAllText("mod.debug.log", ""); // restart log.
            HarmonyHelper.DoOnHarmonyReady(InstallHarmony); 
            LoadingManager.instance.m_levelPreLoaded += CheckMedianCommons.Init;
            CheckMedianCommons.Init();
        }

        [UsedImplicitly]
        public void OnDisabled() {
            LoadingManager.instance.m_levelPreLoaded -= CheckMedianCommons.Init;
            UninstallHarmony();
        }

        #region Harmony
        bool installed = false;
        const string HarmonyId = "CS.kian.DirectConnectRoads";

        [MethodImpl(MethodImplOptions.NoInlining)]
        void InstallHarmony() {
            if (!installed) {
                Extensions.Log("DirectConnectRoads Patching...", true);
#if DEBUG
                //HarmonyInstance.DEBUG = true;
#endif
                Harmony harmony = new Harmony(HarmonyId);
                harmony.PatchAll(GetType().Assembly);
                Extensions.Log("DirectConnectRoads Patching Completed!", true);
                installed = true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void UninstallHarmony() {
            if (installed) {
                Harmony harmony = new Harmony(HarmonyId);
                harmony.UnpatchAll(HarmonyId);
                Extensions.Log("DirectConnectRoads patches Reverted.", true);
                installed = false;
            }
        }
        #endregion
    }
}
