using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MSDFText
{
    public struct MeshInfo
    {
        public Vector3[] positions;
        public Vector2[] uvs;

        public MeshInfo(Vector3[] positions, Vector2[] uvs)
        {
            this.positions = positions;
            this.uvs = uvs;
        }
    }

    public class MSDFTextMesh
    {
        public MSDFTextMesh()
        {
        }
        

        public static MeshInfo GetMeshInfo(string text, MSDFFontData fontData, bool flipY)
        {
            var textureWidth = fontData.common.scaleW;
            var textureHeight = fontData.common.scaleH;

            var glyphs = TextLayout.GetVisibleGlyphs(text, fontData);

            var positions = VertexUtil.CreateVerticesFromGlyphs(glyphs);
            var uvs = VertexUtil.CreateUVsFromGlyphs(glyphs, textureWidth, textureHeight, flipY);

            return new MeshInfo(positions.ToArray(), uvs.ToArray());
        }
    } 
}
