using DirectConnectRoads.Patches;
using KianCommons;
using HarmonyLib;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using DirectConnectRoads.Util;

namespace DirectConnectRoads.LifeCycle {
    public static class LifeCycle {
        public static void Load() {
            Log.Debug("LifeCycle.Load() called");
            CheckMedianCommons.Init();
            HarmonyUtil.InstallHarmony(HarmonyId);
            NetInfoUtil.LoadDCTextures();
            NetInfoUtil.FixMaxTurnAngles();
            NetInfoUtil.FixDCFlags();
        }

        public static void Unload() {
            Log.Debug("LifeCycle.Unload() called");
            HarmonyUtil.UninstallHarmony(HarmonyId);
            NetInfoUtil.RestoreFlags();
            NetInfoUtil.RestoreMaxTurnAngles();
            NetInfoUtil.UnloadDCTextures();
        }
        const string HarmonyId = "CS.kian.DirectConnectRoads";
    }
}