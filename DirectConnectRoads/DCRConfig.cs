namespace DirectConnectRoads {
    using System.IO;
    using System;
    using System.Xml.Serialization;
    using ColossalFramework.IO;
    using KianCommons;
    using System.Collections.Generic;

    public class DCRConfig {
        public List<string> Exemptions = new List<string>();
        public bool GenerateMedians = true;
        public bool RemoveDCRestrictionsAngle = true;
        public bool RemoveDCRestrictionsTL = true;
        public bool RemoveDCRestrictionsTransition = true;

        public const string FILE_NAME = "DCRConfig.xml";
        public static string FilePath => Path.Combine(DataLocation.localApplicationData, FILE_NAME);
        static XmlSerializer ser_ => new XmlSerializer(typeof(DCRConfig));

        static DCRConfig config_;
        static public DCRConfig Config => config_ ??= Deserialize() ?? new DCRConfig();
        public static void Reset() => config_ = new DCRConfig();

        public void Serialize() {
            try {
                using (FileStream fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
                    ser_.Serialize(fs, this);
            } catch(Exception ex) { ex.Log(); }
        }

        public static DCRConfig Deserialize() {
            try {
                if (File.Exists(FilePath)) {
                    using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
                        return ser_.Deserialize(fs) as DCRConfig;
                }
            } catch (Exception ex) { ex.Log(); }
            return null;
        }
    }
}