using DirectConnectRoads.Patches;
using DirectConnectRoads.Util;
using HarmonyLib;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DirectConnectRoads.LifeCycle {
    public static class LifeCycle {
        public static void Load() {
            CheckMedianCommons.Init();
            InstallHarmony();
            NetInfoUtil.LoadDCTextures();
            NetInfoUtil.FixMaxTurnAngles();
        }

        public static void Unload() {
            UninstallHarmony();
            NetInfoUtil.RestoreMaxTurnAngles();
            NetInfoUtil.UnloadDCTextures();
        }

        #region Harmony
        static bool installed = false;
        const string HarmonyId = "CS.kian.DirectConnectRoads";

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void InstallHarmony() {
            if (!installed) {
                Extensions.Log("DirectConnectRoads Patching...", true);
#if DEBUG
                //HarmonyInstance.DEBUG = true;
#endif
                Harmony harmony = new Harmony(HarmonyId);
                harmony.PatchAll();
                Extensions.Log("DirectConnectRoads Patching Completed!", true);
                installed = true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void UninstallHarmony() {
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