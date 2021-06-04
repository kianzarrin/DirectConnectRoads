extern alias UUILib;

namespace DirectConnectRoads.UI {
    using ColossalFramework;
    using ColossalFramework.UI;
    using ICities;
    using System;
    using UUILib::UnifiedUI.Helpers;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using KianCommons;
    using KianCommons.UI;
    using KianCommons.Tool;
    using System.Collections;
    using DirectConnectRoads.Util;

    public class DCRTool : KianToolBase<DCRTool> {
        UIComponent button_;

        protected override void Awake() {
            try {
                base.Awake();
                string sprites = UUIHelpers.GetFullPath<LifeCycle.Mod>("B.png");
                Debug.Log("[UUIExampleMod] ExampleTool.Awake() sprites=" + sprites);
                button_ = UUIHelpers.RegisterToolButton(
                    name: "DirectConnectRoad",
                    groupName: null, // default group
                    tooltip: "Direct Connect Road",
                    spritefile: sprites,
                    tool: this);
            } catch (Exception ex) {
                Debug.LogException(ex);
                UIView.ForwardException(ex);
            }
        }

        protected override void OnDestroy() {
            button_?.Destroy();
            button_ = null;
            base.OnDestroy();
        }

        NetInfo GetHoveredNetInfo() {
            var info = HoveredSegmentID.ToSegment().Info;
            bool supported = 
                info && info.IsRoad() && 
                !NetInfoUtil.UnsupportedRoadWithTrackTable.Contains(info) &&
                API.InvokeShouldManageDCNodes(info, HoveredSegmentID , 0);

            if (!supported)
                return null;
            else 
                return info;
        }

        protected override void OnPrimaryMouseClicked() {
            var info = GetHoveredNetInfo();
            if (!info) return;

            var excemptions = DCRConfig.Config.Exemptions;
            if (excemptions.Contains(info.name))
                excemptions.Remove(info.name);
            else
                excemptions.Add(info.name);

            DCRConfig.Config.Serialize();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            var info = GetHoveredNetInfo();
            if (!info) return;

            bool exempt = DCRConfig.Config.Exemptions.Contains(info.name);
            Color color = exempt ? Color.red : Color.green;
            RenderUtil.RenderSegmnetOverlay(cameraInfo, HoveredSegmentID, color);
        }

        protected override void OnToolUpdate() {
            base.OnToolUpdate();
            var info = GetHoveredNetInfo();
            if (!info) {
                base.ShowToolInfo(false, "", default);
                return;
            }

            bool exempt = DCRConfig.Config.Exemptions.Contains(info.name);
            if (!exempt)
                ShowToolInfo(true, "click to exempt asset from DCR", HitPos);
            else
                ShowToolInfo(true, "click to manage asset by DCR", HitPos);
        }

        protected override void OnSecondaryMouseClicked() {
            enabled = false;
        }
    }
}
