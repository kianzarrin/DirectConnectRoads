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

    internal class DCRTool : KianToolBase<DCRTool> {
        UIComponent button_;

        protected override void Awake() {
            try {
                base.Awake();
                Log.Called();
                string sprites = UUIHelpers.GetFullPath<LifeCycle.Mod>("uui_cjm_dcr.png");
                Log.Info("sprites=" + sprites);
                button_ = UUIHelpers.RegisterToolButton(
                    name: "DirectConnectRoad",
                    groupName: null, // default group
                    tooltip: "Direct Connect Road",
                    spritefile: sprites,
                    tool: this);
            } catch (Exception ex) {
                ex.Log();
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

            if (NetInfoUtil.IsExempt(info))
                NetInfoUtil.UnExempt(info);
            else
                NetInfoUtil.Exempt(info);

            DCRConfig.Config.Serialize();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            var info = GetHoveredNetInfo();
            if (!info) return;

            bool exempt = DCRConfig.Config.ExemptionsSet.Contains(info.name);
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

            bool exempt = DCRConfig.Config.ExemptionsSet.Contains(info.name);
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
