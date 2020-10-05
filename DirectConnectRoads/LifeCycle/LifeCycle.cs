using DirectConnectRoads.Patches;
using KianCommons;
using DirectConnectRoads.Util;
using CitiesHarmony.API;

namespace DirectConnectRoads.LifeCycle {
    public static class LifeCycle {
        const string HarmonyId = "CS.kian.DirectConnectRoads";
        public static bool Loaded;

        #region extension calls
        public static void Enable() {
            Loaded = false;
            HarmonyHelper.EnsureHarmonyInstalled();
            LoadingManager.instance.m_levelPreLoaded += PreLoad; //install harmony
            LoadingManager.instance.m_simulationDataReady += SimulationDataReady; // create DC textures on first load
            LoadingManager.instance.m_levelPreUnloaded += ExitToMainMenu; // undo DC textures when quite to main menue.
            if (HelpersExtensions.InGame) 
                HotReload();
        }

        public static void Disable() {
            LoadingManager.instance.m_levelPreLoaded -= PreLoad; //install harmony
            LoadingManager.instance.m_simulationDataReady -= SimulationDataReady;
            LoadingManager.instance.m_levelPreUnloaded -= ExitToMainMenu;
            HarmonyUtil.UninstallHarmony(HarmonyId);
            ExitToMainMenu(); // in case of hot unload

        }

        //public static void OnLevelLoaded(LoadMode mode) {
        //    // after level is loaded.
        //}

        //public static void OnLevelUnloading() {
        //    // unloading to main menue or load new level.
        //}

        #endregion

        public static void HotReload() {
            PreLoad();
            SimulationDataReady();
        }

        // first thing that happens when start game/editor from main menue or load another game.
        public static void PreLoad() {
            Log.Debug("LifeCycle.PreLoad() called");
            HarmonyUtil.InstallHarmony(HarmonyId); // if not installed already.
        }

        public static void SimulationDataReady() {
            if (Loaded) return; // protect against reload
            Log.Debug("LifeCycle.SimulationDataReady() called");
            CheckMedianCommons.Init();
            NetInfoUtil.LoadDCTextures();
            NetInfoUtil.FixMaxTurnAngles();
            NetInfoUtil.FixDCFlags();
            Loaded = true;
        }

        public static void ExitToMainMenu() {
            if (!Loaded) return; // protect against Disabling mod from main menu.
            Log.Debug("LifeCycle.Quite() called");
            NetInfoUtil.RestoreFlags();
            NetInfoUtil.RestoreMaxTurnAngles();
            NetInfoUtil.UnloadDCTextures();
            Loaded = false;
        }
    }
}