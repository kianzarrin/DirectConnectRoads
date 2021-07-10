
namespace DirectConnectRoads.LifeCycle {
    using ColossalFramework.UI;
    using ICities;
    using static KianCommons.ReflectionHelpers;
    using DirectConnectRoads.Util;

    public static class DCRSettings {
        public static UICheckBox AddToggle(this UIHelperBase helper, string text, string tooltip, string fieldName) {
            bool defaultValue = (bool)GetFieldValue(DCRConfig.Config, fieldName);

            void OnValueChanged(bool val) {
                SetFieldValue(DCRConfig.Config, fieldName, val);
                DCRConfig.Config.Serialize();
            }

            var ret = helper.AddCheckbox(text, defaultValue, OnValueChanged) as UICheckBox;
            ret.tooltip = tooltip;
            return ret;
        }

        public static void OnSettingsUI(UIHelper helper) {
            {
                UIButton button = helper.AddButton("Refresh all junctions (Resolve blue clippings)", RefreshNetworks) as UIButton;
                button.tooltip = "might take a while";
                void RefreshNetworks() => SimulationManager.instance.AddAction(NetInfoUtil.FullUpdateAllNetworks);
            }

            var g = helper.AddGroup("Startup options (requires restart)");
            g.AddToggle(
                text: "Generate junction medians",
                tooltip: "Generates medians for roads that don't have it (unsupported roads are skipped. see log)",
                fieldName: nameof(DCRConfig.GenerateMedians));
            g.AddToggle(
                text: "Remove median angle restrictions",
                tooltip: "Removes the 'Maximum Turn Angle' for roads that already have a continues junction median.",
                fieldName: nameof(DCRConfig.RemoveDCRestrictionsAngle));
            g.AddToggle(
                text: "Remove Traffic light restrictions",
                tooltip: "Removes the traffic light requirement for roads that already have a continues junction median.",
                fieldName: nameof(DCRConfig.RemoveDCRestrictionsTransition));
            g.AddToggle(
                text: "Remove 'Transition' restrictions",
                tooltip: "Removes the 'urban to highway transition' requirement for roads that already have a continues junction median.",
                fieldName: nameof(DCRConfig.RemoveDCRestrictionsTransition));
        }
    }
}
