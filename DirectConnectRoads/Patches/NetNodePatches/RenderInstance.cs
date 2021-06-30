using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using KianCommons;
using KianCommons.Patches;

namespace DirectConnectRoads.Patches.NetNodePatches {
    [HarmonyPatch()]
    public static class RenderInstance {
        // RenderInstance(RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, Flags flags, ref uint instanceIndex, ref RenderManager.Instance data)
        static MethodInfo TargetMethod() => typeof(global::NetNode)
            .GetMethod("RenderInstance", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new Exception("could not find NetNode.RenderInstance");

        //static bool Prefix(ushort nodeID){}
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
            try {
                var codes = TranspilerUtils.ToCodeList(instructions);
                CheckMedianCommons.ApplyCheckMedian(codes, original, occurance: 1);
                return codes;
            } catch (Exception e) {
                e.Log();
                throw e;
            }
        }
    } // end class
} // end name space