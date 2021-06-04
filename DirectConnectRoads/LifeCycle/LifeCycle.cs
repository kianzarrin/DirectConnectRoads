using DirectConnectRoads.Patches;
using KianCommons;
using DirectConnectRoads.Util;
using CitiesHarmony.API;
using System;

namespace DirectConnectRoads.LifeCycle {
    public static class LifeCycle {
        const string HarmonyId = "CS.kian.DirectConnectRoads";
        public static bool Loaded;

        #region extension calls
        public static void Enable() {
            Log.Debug(Environment.StackTrace);
            Loaded = false;
            HarmonyHelper.EnsureHarmonyInstalled();
            LoadingManager.instance.m_levelPreLoaded += PreLoad; //install harmony
            LoadingManager.instance.m_simulationDataReady += SimulationDataReady; // create DC textures on first load
            LoadingManager.instance.m_levelPreUnloaded += ExitToMainMenu; // undo DC textures when quite to main menue.
            if (!Helpers.InStartupMenu) 
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
            AfterLoad();
        }

        // first thing that happens when start game/editor from main menue or load another game.
        public static void PreLoad() {
            try {
                Log.Debug("LifeCycle.PreLoad() called");
                HarmonyUtil.InstallHarmony(HarmonyId); // if not installed already.
            } catch (Exception e) {
                Log.Exception(e);
            }
        }

        public static void SimulationDataReady() {
            try {
                Log.Debug("LifeCycle.SimulationDataReady() called");
                if (!Loaded) {
                    NetInfoUtil.LoadDCTextures();
                    NetInfoUtil.FixMaxTurnAngles();
                    NetInfoUtil.FixDCFlags();
                }

                Loaded = true;
            }
            catch (Exception e) {
                Log.Exception(e);
            }
        }

        public static void AfterLoad() {
            SimulationManager.instance.AddAction(delegate () {
                // TODO: which line to uncomment:
                // NetInfoUtil.UpdateAllNetworkRenderers();
                // NetInfoUtil.FastUpdateAllNetworks();
                // NetInfoUtil.FullUpdateAllNetworks();
                NetInfoUtil.UpdateAllNodeRenderers();
            });
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