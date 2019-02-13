using MSDFText;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSDFText
{
    public class VertexUtil
    {
        public static List<float> CreatePagesFromGlyphs(List<ProcessedGlyph> glyphs)
        {
            var pages = new List<float>(glyphs.Count * 4);

            var i = 0;

            foreach (var pg in glyphs)
            {
                var id = pg.glyph.page;

                pages.Add(id);
                pages.Add(id);
                pages.Add(id);
                pages.Add(id);
            }

            return pages;
        }

        public static List<Vector2> CreateUVsFromGlyphs(List<ProcessedGlyph> glyphs, float textureWidth, float textureHeight, bool flipY)
        {
            var uvs = new List<Vector2>(glyphs.Count * 4);

            var i = 0;

            foreach (var pg in glyphs)
            {
                var bitmap = pg.glyph;
                var bw = (bitmap.x + bitmap.width);
                var bh = (bitmap.y + bitmap.height);

                var u0 = bitmap.x / textureWidth; // x
                var v1 = bitmap.y / textureHeight; // y
                var u1 = bw / textureWidth; // w
                var v0 = bh / textureHeight; // h

                if (flipY)
                {
                    v1 = (textureHeight - bitmap.y) / textureHeight;
                    v0 = (textureHeight - bh) / textureHeight;
                }

                uvs.Add(new Vector2(u0, v1));
                uvs.Add(new Vector2(u0, v0));
                uvs.Add(new Vector2(u1, v0));
                uvs.Add(new Vector2(u1, v1));

            }

            return uvs;
        }

        public static List<Vector3> CreateVerticesFromGlyphs(List<ProcessedGlyph> glyphs)
        {
            var positions = new List<Vector3>(glyphs.Count * 4);

            var i = 0;
            var width = 0f;
            //var offset = new Vector2(Random.Range(-1000f, 1000f), Random.Range(-1000f, 1000f));
            foreach (var pg in glyphs)
            {
                var bitmap = pg.glyph;

                var x = pg.position.x + bitmap.xoffset;
                var y = pg.position.y - bitmap.yoffset;

                var w = bitmap.width;
                var h = bitmap.height;

                positions.Add(new Vector3(x, y, 0)); // TL
                positions.Add(new Vector3(x, y - h, 0)); // BL
                positions.Add(new Vector3(x + w, y - h, 0)); // RB
                positions.Add(new Vector3(x + w, y, 0)); // RT

                width += w;

                ++i;
            }

            return positions;
        }

        public static List<int> CreateTriangleIndices(int count, bool clockwise, int start = 0)
        {
            var indicesByDirection = clockwise ? new[] { 0, 3, 1, 1, 3, 2 } : new[] { 0, 1, 3, 3, 1, 2 };

            var numIndices = count * 6;
            var indices = new List<int>(numIndices);

            for (int i = 0, j = 0; i < numIndices; i += 6, j += 4)
            {
                var x = i + start;

                indices.Add(j + indicesByDirection[0]);
                indices.Add(j + indicesByDirection[1]);
                indices.Add(j + indicesByDirection[2]);
                indices.Add(j + indicesByDirection[3]);
                indices.Add(j + indicesByDirection[4]);
                indices.Add(j + indicesByDirection[5]);
            }

            return indices;
        }
    } 
}
