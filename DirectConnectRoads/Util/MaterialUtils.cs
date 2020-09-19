using System;
using System.Linq;
using UnityEngine;
using KianCommons;

// TODO check out material.MainTextureScale
// regarding weird nodes, what if we return a copy of the material?
// Loading screens Mod owner wrote this about LODs: https://steamcommunity.com/workshop/filedetails/discussion/667342976/1636416951459546732/
namespace DirectConnectRoads.Util {
    using static TextureUtils;
    public static class MaterialUtils {
        public static Texture2D TryGetTexture2D(this Material material, int textureID) {
            try {
                if (material.HasProperty(textureID))
                {
                    Texture texture = material.GetTexture(textureID);
                    if (texture is Texture2D)
                        return texture as Texture2D;
                }
            }
            catch { }
            //Log.Info($"Warning: failed to get {getTexName(textureID)} texture from material :" + material.name);
            return null;
        }

        //public static NetInfo.Segment GetSegment(NetInfo info, int textureID) {
        //    foreach (var segmentInfo in info.m_segments ?? Enumerable.Empty<NetInfo.Segment>()) {
        //        if (!segmentInfo.CheckFlags(NetSegment.Flags.None, out _))
        //            continue;
        //        Log.Debug($"segmentInfo={segmentInfo.m_mesh.name} " +
        //            $"texture={segmentInfo.m_segmentMaterial.TryGetTexture2D(textureID)}", false);
        //        if (!segmentInfo.m_segmentMaterial.TryGetTexture2D(textureID))
        //            continue;
        //        Log.Debug($"GetSegment -> segmentInfo={segmentInfo.m_mesh.name} " +
        //            $"texture={segmentInfo.m_segmentMaterial.TryGetTexture2D(textureID)}", false);

        //        return segmentInfo;
        //    }
        //    return null;
        //}

        //public static Material ContinuesMedianMaterial(NetInfo info, bool lod = false) {
        //    if (info == null) throw new ArgumentNullException("info");
        //    var segment = GetSegment(info, ID_APRMap);
        //    var segMaterial = segment?.m_material;
        //    return segMaterial ? new Material(segMaterial) : null;

        //}

        //public static Mesh ContinuesMedianMesh(NetInfo info, bool lod = false) {
        //    if (info == null) throw new ArgumentNullException("info");
        //    var segment = GetSegment(info, ID_APRMap);
        //    return segment?.m_mesh;
        //}
    } // end class
} // end namesapce

