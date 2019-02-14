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
        public float width;
        public float height;

        public MeshInfo(Vector3[] positions, Vector2[] uvs, float width, float height)
        {
            this.positions = positions;
            this.uvs = uvs;
            this.width = width;
            this.height = height;
        }
    }

    public class MSDFTextMesh
    {
        public MSDFTextMesh()
        {
        }
        

        public static MeshInfo GetMeshInfo(string text, MSDFFontData fontData, bool flipY)
        {
            var glyphs = TextLayout.GetVisibleGlyphs(text, fontData);
            var positions = VertexUtil.CreateVerticesFromGlyphs(glyphs);
            var uvs = VertexUtil.CreateUVsFromGlyphs(glyphs, fontData.common.scaleW, fontData.common.scaleH, flipY);
            var width = positions[positions.Count - 1].x - positions[0].x;
            var maxY = positions.Max(v => v.y);
            var minY = positions.Min(v => v.y);
            var height = maxY - minY;
            Debug.Log($"max y : {maxY}, min y : {minY}, height : {height}");

            return new MeshInfo(positions.ToArray(), uvs.ToArray(), width, height);
        }
    } 
}
