using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MSDFText
{
    public struct Metrics
    {
        public int start { get; set; }
        public int end { get; set; }
        public float width { get; set; }
    }

    public class ProcessedGlyph
    {
        public Vector2 position { get; set; }
        public Glyph glyph { get; set; }
        public int index { get; set; }
        public int line { get; set; }
    }

    public class TextLayout
    {
        private static readonly List<string> X_HEIGHTS = new List<string>() { "x", "e", "a", "o", "n", "s", "r", "c", "u", "m", "v", "w", "z" };
        private static readonly List<string> CAP_HEIGHTS = new List<string>() { "H", "I", "N", "E", "F", "K", "L", "T", "U", "V", "W", "X", "Y", "Z" };
        private static readonly int TAB_ID = Encoding.ASCII.GetBytes("\t")[0];
        private static readonly int SPACE_ID = Encoding.ASCII.GetBytes(" ")[0];

        private float _tabSize = 4f;
        private Glyph _fallbackSpaceGlyph;
        private Glyph _fallbackTabGlyph;

        private float _width { get; set; }
        private float _height { get; set; }
        private float _descender { get; set; }
        private float _baseline { get; set; }
        private float _xHeight { get; set; }
        private float _capHeight { get; set; }
        private float _lineHeight { get; set; }
        private float _ascender { get; set; }
        private int _linesTotal { get; set; }

        public List<ProcessedGlyph> glyphs { get; private set; }

        public enum Align
        {
            LEFT,
            CENTER,
            RIGHT
        }

        public TextLayout() : this("", null)
        {

        }

        public TextLayout(string text, MSDFFontData fontData, int tabSize = 4, int width = 0, float lineHeight = 0f, float letterSpacing = 0f, Align alignType = Align.LEFT)
        {
            glyphs = new List<ProcessedGlyph>();

            Update(text, fontData, tabSize, width, lineHeight, letterSpacing);
        }

        private void Update(string text, MSDFFontData fontData, int tabSize = 4, int width = 0, float lineHeight = 0f, float letterSpacing = 0f, Align alignType = Align.LEFT)
        {
            SetupSpaceGlyphs(fontData);

            var lines = WordWrapper.GetLines(text, fontData);
            var minWidth = width;

            glyphs.Clear();

            var maxLineWidth = lines.Max(metrics => metrics.width);

            var x = 0f;
            var y = 0f;
            var lh = 0f == lineHeight ? fontData.common.lineHeight : lineHeight;
            var baseline = fontData.common.baseLine;
            var descender = lh - baseline;
            var spacing = letterSpacing;
            var height = lh * lines.Count() - descender;
            var align = alignType;

            y -= height;

            _width = maxLineWidth;
            _height = height;
            _descender = lineHeight - baseline;
            _baseline = baseline;
            _xHeight = GetXHeight(fontData);
            _capHeight = GetCapHeight(fontData);
            _lineHeight = lineHeight;
            _ascender = lineHeight - descender - _xHeight;

            int lineIndex = 0;

            foreach (var line in lines)
            {
                var start = line.start;
                var end = line.end;
                var lineWidth = line.width;
                Glyph lastGlyph = null;

                for (var i = start; i < end; ++i)
                {
                    var id = MSDFFontData.GetCharCode(text[i]);

                    var glyph = fontData.GetGlyphById(id);

                    if (null != glyph)
                    {
                        if (null != lastGlyph)
                        {
                            x += fontData.GetKerningAmount(lastGlyph.id, glyph.id);
                        }

                        var tx = x;

                        if (Align.CENTER == align)
                        {
                            tx += (maxLineWidth - lineWidth) * 0.5f;
                        }
                        else if (Align.RIGHT == align)
                        {
                            tx += (maxLineWidth - lineWidth);
                        }

                        glyphs.Add(new ProcessedGlyph()
                        {
                            position = new Vector2(tx, y),
                            glyph = glyph,
                            index = i,
                            line = lineIndex
                        });

                        x += glyph.xadvance + letterSpacing;
                        lastGlyph = glyph;
                    }
                }

                y += lineHeight;
                x = 0;

                ++lineIndex;
            }

            _linesTotal = lines.Count;
        }

        private void SetupSpaceGlyphs(MSDFFontData fontData)
        {
            var space = fontData.GetGlyphById(SPACE_ID);

            if (null == space)
            {
                space = fontData.GetMGlyph();
            }

            if (null == space)
            {
                space = fontData.GetGlyphByIndex(0);
            }

            var tabWidth = _tabSize * space.xadvance;

            _fallbackSpaceGlyph = space;

            _fallbackTabGlyph = new Glyph()
            {
                x = 0,
                y = 0,
                xadvance = tabWidth,
                id = TAB_ID,
                xoffset = 0,
                yoffset = 0,
                width = 0,
                height = 0,
                page = space.page,
                channel = space.channel,
                character = space.character,
                index = space.index,
            };
        }

        private static float GetXHeight(MSDFFontData fontData)
        {
            foreach (var str in X_HEIGHTS)
            {
                var id = MSDFFontData.GetCharCode(str);

                if (fontData.HasChar(id))
                {
                    return fontData.GetGlyphById(id).height;
                }
            }

            return 0f;
        }

        private static float GetCapHeight(MSDFFontData fontData)
        {
            foreach (var str in CAP_HEIGHTS)
            {
                var id = MSDFFontData.GetCharCode(str);

                if (fontData.HasChar(id))
                {
                    return fontData.GetGlyphById(id).height;
                }
            }

            return 0f;
        }

        public static List<ProcessedGlyph> GetVisibleGlyphs(string text, MSDFFontData fontData, float tabSize = 4f, int width = 0, float lineHeight = 0f, float letterSpacing = 0f, Align alignType = Align.LEFT)
        {
            // setup space and tab
            var space = fontData.GetGlyphById(SPACE_ID);

            if (null == space)
            {
                space = fontData.GetMGlyph();
            }

            if (null == space)
            {
                space = fontData.GetGlyphByIndex(0);
            }

            var fallbackSpaceGlyph = space;

            var fallbackTabGlyph = new Glyph()
            {
                x = 0,
                y = 0,
                xadvance = tabSize * space.xadvance,
                id = TextLayout.TAB_ID,
                xoffset = 0,
                yoffset = 0,
                width = 0,
                height = 0,
                page = space.page,
                channel = space.channel,
                character = space.character,
                index = space.index,
            };

            // calculate layouts
            var glyphs = new List<ProcessedGlyph>();

            var lines = WordWrapper.GetLines(text, fontData);
            var minWidth = width;

            glyphs.Clear();

            var maxLineWidth = lines.Max(metrics => metrics.width);

            var x = 0f;
            var y = 0f;
            var lh = 0f == lineHeight ? fontData.common.lineHeight : lineHeight;
            var descender = lh - fontData.common.baseLine;
            var height = lh * lines.Count() - descender;

            y -= height;

            int lineIndex = 0;

            foreach (var line in lines)
            {
                var start = line.start;
                var end = line.end;
                var lineWidth = line.width;
                Glyph lastGlyph = null;

                for (var i = start; i < end; ++i)
                {
                    var id = MSDFFontData.GetCharCode(text[i]);

                    Glyph glyph;

                    if (id == TAB_ID)
                    {
                        glyph = fallbackTabGlyph;
                    }
                    else if (id == SPACE_ID)
                    {
                        glyph = fallbackSpaceGlyph;
                    }
                    else
                    {
                        glyph = fontData.GetGlyphById(id);
                    }

                    if (null != glyph)
                    {
                        if (null != lastGlyph)
                        {
                            x += fontData.GetKerningAmount(lastGlyph.id, glyph.id);
                        }

                        var tx = x;

                        if (Align.CENTER == alignType)
                        {
                            tx += (maxLineWidth - lineWidth) * 0.5f;
                        }
                        else if (Align.RIGHT == alignType)
                        {
                            tx += (maxLineWidth - lineWidth);
                        }

                        glyphs.Add(new ProcessedGlyph()
                        {
                            position = new Vector2(tx, y),
                            glyph = glyph,
                            index = i,
                            line = lineIndex
                        });

                        x += glyph.xadvance + letterSpacing;
                        lastGlyph = glyph;
                    }
                }

                y += lineHeight;
                x = 0;

                ++lineIndex;
            }

            return glyphs.Where(g => g.glyph.width * g.glyph.height > 0).ToList();
        }
    }
}
