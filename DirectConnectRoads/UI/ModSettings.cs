namespace DirectConnectRoads.UI {
    using ColossalFramework;
    using ColossalFramework.UI;
    using ICities;
    using System;
    using UnifiedUI.Helpers;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using KianCommons;
    using KianCommons.UI;

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
