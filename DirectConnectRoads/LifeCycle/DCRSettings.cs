
namespace DirectConnectRoads.LifeCycle {
    using ColossalFramework.UI;
    using ICities;
    using static KianCommons.ReflectionHelpers;
    using DirectConnectRoads.Util;
    using System;
    using KianCommons;

    public static class DCRSettings {
        static UICheckBox AddToggle(this UIHelperBase helper, string text, string tooltip, string fieldName, Action OnRefresh) {
            bool defaultValue = (bool)GetFieldValue(DCRConfig.Config, fieldName);

            void OnValueChanged(bool val) {
                SetFieldValue(DCRConfig.Config, fieldName, val);
                DCRConfig.Config.Serialize();
                OnRefresh?.Invoke();
            }

            var ret = helper.AddCheckbox(text, defaultValue, OnValueChanged) as UICheckBox;
            ret.tooltip = tooltip;
            return ret;
        }

        static UIButton AddButton(this UIHelperBase helper, string text, string tooltip, OnButtonClicked action) {
            UIButton button = helper.AddButton(text, action) as UIButton;
            button.tooltip = tooltip;
            return button;
        }

        static void RefreshDC() {
            try {
                if (Helpers.InStartupMenu) return;
                Log.Called();

                SimulationManager.instance.ForcedSimulationPaused = true;
                NetInfoUtil.RestoreFlags();
                NetInfoUtil.RestoreMaxTurnAngles();
                NetInfoUtil.UnloadDCTextures();

                NetInfoUtil.GenerateDCTextures();
                NetInfoUtil.FixMaxTurnAngles();
                NetInfoUtil.FixDCFlags();
            } catch (Exception ex) {
                ex.Log();
            } finally {
                SimulationManager.instance.ForcedSimulationPaused = false;
            }

            SimulationManager.instance.AddAction(delegate () {
                NetInfoUtil.UpdateAllNodeRenderers();
            });
        }

        static void RefreshDCFlags() {
            try {
                if (Helpers.InStartupMenu) return;
                Log.Called();

                SimulationManager.instance.ForcedSimulationPaused = true;
                NetInfoUtil.RestoreFlags();
                NetInfoUtil.RestoreMaxTurnAngles();

                NetInfoUtil.FixMaxTurnAngles();
                NetInfoUtil.FixDCFlags();
            } catch (Exception ex) {
                ex.Log();
            } finally {
                SimulationManager.instance.ForcedSimulationPaused = false;
            }

            SimulationManager.instance.AddAction(delegate () {
                NetInfoUtil.UpdateAllNodeRenderers();
            });
        }

        static void RefreshNetworks() => SimulationManager.instance.AddAction(NetInfoUtil.FullUpdateAllRoadJunctions);

        public static void OnSettingsUI(UIHelper helper) {
            {
                var g = helper.AddGroup("Automation");
                g.AddToggle(
                    text: "Generate junction medians",
                    tooltip: "Generates medians for roads that don't have it (unsupported roads are skipped. see log)",
                    fieldName: nameof(DCRConfig.GenerateMedians),
                    OnRefresh: RefreshDC);
                g.AddToggle(
                    text: "Remove median angle restrictions",
                    tooltip: "Removes the 'Maximum Turn Angle' for roads that already have a continues junction median.",
                    fieldName: nameof(DCRConfig.RemoveDCRestrictionsAngle),
                    OnRefresh: RefreshDCFlags);
                g.AddToggle(
                    text: "Remove Traffic light restrictions",
                    tooltip: "Removes the traffic light requirement for roads that already have a continues junction median.",
                    fieldName: nameof(DCRConfig.RemoveDCRestrictionsTL),
                    OnRefresh: RefreshDCFlags);
                g.AddToggle(
                    text: "Remove 'Transition' restrictions",
                    tooltip: "Removes the 'urban to highway transition' requirement for roads that already have a continues junction median.",
                    fieldName: nameof(DCRConfig.RemoveDCRestrictionsTransition),
                    OnRefresh: RefreshDCFlags);
            }
            {
                var g2 = helper.AddGroup("Debug");
                g2.AddButton("Refresh all road junctions (Resolve blue clippings)", "might take a while", RefreshNetworks);
                g2.AddButton("Regenerate meshes", "might take a while", RefreshDC);
            }

        }
    }
}
