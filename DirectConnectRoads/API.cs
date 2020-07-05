namespace DirectConnectRoads {
    using System;
    using DirectConnectRoads.LifeCycle;
    public static class API {
        public static Version ModVersion => Mod.ModVersion;
        public static bool IsModEnabled => Mod.IsEnabled;
    }
}
