using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using KianCommons;
using KianCommons.Plugins;
using System;

namespace DirectConnectRoads.Patches {
    using DirectConnectRoads.Util;

    using static KianCommons.Patches.TranspilerUtils;
    public static class CheckMedianCommons {
        public static bool ShouldConnectMedian(
            ushort nodeId,
            int nodeInfoIDX,
            in RenderManager.Instance data) {
            try {
                ushort sourceSegmentID = nodeId.ToNode().GetSegment(data.m_dataInt0 & 7);
                int targetSegmentIDX = data.m_dataInt0 >> 4;
                ushort targetSegmentID = nodeId.ToNode().GetSegment(targetSegmentIDX);
                NetInfo info = sourceSegmentID.ToSegment().Info;
                NetInfo.Node nodeInfo = info.m_nodes[nodeInfoIDX];
                if (!info.IsRoad())
                    return true; // ignore
                if (!DCUtil.IsMedian(nodeInfo, info)) {
                    //Log.Debug($"not a median: node:{nodeId} segmentId:{sourceSegmentID} connect_group:{nodeInfo.m_connectGroup} " +
                    //    $"vehcileTypes:{info.m_vehicleTypes}", false);
                    return true; // ignore.
                }

                if (info.IsAdaptive()) {
                    if (!nodeInfo.HideBrokenARMedians())
                        return true; // ignore
                } else {
                    if (NetInfoUtil.UnsupportedRoadWithTrackTable.Contains(info)) {
                        //Log.Debug($"{info} is UnsupportedRoadWithTrack");
                        return true; // ignore
                    }
                    if (!info.IsNormalSymetricalTwoWay())
                        return true; // ignore
                }

                bool autogenerated = NetInfoUtil.AddedNodes.Contains(nodeInfo);
                NetInfo sourceInfo = sourceSegmentID.ToSegment().Info;
                NetInfo targetInfo = targetSegmentID.ToSegment().Info;
                if (autogenerated && !Connects(sourceInfo, targetInfo)) {
                    return false; // ignore autogenerated
                }

                if (DCRConfig.Config.Exemptions.Contains(info.name)) {
                    //Log.Debug($"{info} is Exempt");
                    return !autogenerated; // ignore
                }

                if (!API.InvokeShouldManageDCNodes(info, sourceSegmentID, targetSegmentID)) {
                    //Log.Debug($"should not Manage DC Nodes for {info}");
                    return !autogenerated; // ignore
                }

                return !DirectConnectUtil.OpenMedian(sourceSegmentID, targetSegmentID);
                //.LogRet($"OpenMedian(sourceSegmentID={sourceSegmentID}, targetSegmentID={targetSegmentID})");
            }
            catch (Exception ex) {
                ex.Log();
                return true; // do nothing.
            }
        }

        public static bool Connects(NetInfo source, NetInfo target) {
            if (!source.IsCombatible(target))
                return false;
            if ( (source.m_vehicleTypes ^ target.m_vehicleTypes)  != VehicleInfo.VehicleType.None) 
                return false;
            return true;
        }


        static MethodInfo mShouldConnectMedian => typeof(CheckMedianCommons).GetMethod("ShouldConnectMedian");
        static MethodInfo mCheckRenderDistance => typeof(RenderManager.CameraInfo).GetMethod("CheckRenderDistance");
        static FieldInfo f_m_nodes => typeof(NetInfo).GetField("m_nodes");

        public static void ApplyCheckMedian(List<CodeInstruction> codes, MethodBase method, int occurance) {
            Assertion.Assert(mCheckRenderDistance != null, "mCheckRenderDistance!=null failed");
            Assertion.Assert(mShouldConnectMedian != null, "mShouldConnectMedian!=null failed");
            /*
            --->insert here
            [164 17 - 164 95]
            IL_02c0: ldarg.1      // cameraInfo
            IL_02c1: ldarg.s      data
            IL_02c3: ldfld        valuetype [UnityEngine]UnityEngine.Vector3 RenderManager/Instance::m_position
            IL_02c8: ldloc.s      node // <--= LDLoc_NodeInfo
            IL_02ca: ldfld        float32 NetInfo/Node::m_lodRenderDistance
            IL_02cf: callvirt     instance bool RenderManager/CameraInfo::CheckRenderDistance(valuetype [UnityEngine]UnityEngine.Vector3, float32)
            IL_02d4: brfalse      IL_0405
             */

            CodeInstruction LDArg_NodeID = GetLDArg(method, "nodeID");
            CodeInstruction LDArg_data = GetLDArg(method, "data");

            int index = 0;
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, mCheckRenderDistance), index, counter: occurance);
            Assertion.Assert(index != 0, "index!=0");
            CodeInstruction LDLoc_NodeInfoIDX = Search_LDLoc_NodeInfoIDX(codes, index, counter: 1, dir: -1);

            //seek to <ldarg.s cameraInfo> instruction:
            index = SearchInstruction(codes, GetLDArg(method, "cameraInfo"), index, counter: occurance, dir: -1);

            Label ContinueIndex = GetContinueLabel(codes, index, dir: -1); // IL_029d: br IL_0570
            {
                var newInstructions = new[]{
                    LDArg_NodeID,
                    LDLoc_NodeInfoIDX,
                    LDArg_data,
                    new CodeInstruction(OpCodes.Call, mShouldConnectMedian),
                    new CodeInstruction(OpCodes.Brfalse, ContinueIndex), // if returned value is false then continue to the next iteration of for loop;
                };

                InsertInstructions(codes, newInstructions, index, true);
            } // end block
        } // end method

        public static CodeInstruction Search_LDLoc_NodeInfoIDX(List<CodeInstruction> codes, int index, int counter, int dir) {
            Assertion.Assert(f_m_nodes != null, "f_m_nodes!=null failed");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, f_m_nodes), index, counter: counter, dir: dir);

            var code = codes[index + 1];
            Assertion.Assert(IsLdLoc(code), $"IsLdLoc(code) | code={code}");
            return code;
        }
    }
}
