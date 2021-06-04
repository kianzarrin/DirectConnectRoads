namespace DirectConnectRoads.UI {
    using KianCommons;
    using System;

    public static class ModSettings {

        public static void OnSettingsUI(UIHelper helper) {
            try {
                helper.AddButton("Reset Exemptions", () => DCRConfig.Reset());
            } catch (Exception ex) {
                ex.Log();
            }
        }
    }
}
