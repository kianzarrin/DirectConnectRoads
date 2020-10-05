namespace DirectConnectRoads.LifeCycle
{
    using System;
    using JetBrains.Annotations;
    using ICities;
    using CitiesHarmony.API;
    using KianCommons;
    public class Mod : IUserMod
    {
        public static Version ModVersion => typeof(Mod).Assembly.GetName().Version;
        public static string VersionString => ModVersion.ToString(2);
        public string Name => "Direct Connect Roads V"+ VersionString;
        public string Description => "generate/uses Direct Connect textures if TMPE rules suggests unbroken median";
        public static bool IsEnabled = false;

        [UsedImplicitly]
        public void OnEnabled()
        {
            LifeCycle.Enable();
            IsEnabled = true;
        }

        [UsedImplicitly]
        public void OnDisabled()
        {
            LifeCycle.Disable();
            IsEnabled = false;
        }
    }
}
