using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MSDFText
{
    public static class WordWrapper
    {
        public enum WrapMode
        {
            NONE,
            NO_WRAP,
            PRE,
            GREEDY,
        }

        private static Regex _newline = new Regex(@"\n");
        private static Regex _whitespace = new Regex(@"\s+");
        private static string _newlineChar = "\n";

        public static List<Metrics> GetLines(string text, MSDFFontData fontData, int? start = null, int? end = null, float? width = null, WrapMode mode = WrapMode.NONE, bool monospace = false)
        {
            if (0f == width && WrapMode.NO_WRAP == mode)
            {
                return new List<Metrics>();
            }

            var width_ = (float)(null != width ? width : float.MaxValue);
            var start_ = Mathf.Max(0, (int)(null != start ? start : 0));
            var end_ = (int)(null != end ? end : text.Length);

            if (WrapMode.PRE == mode)
            {
                return Pre(fontData, text, start_, end_ ,width_, monospace);
            }
            else
            {
                return Greedy(fontData, text, start_, end_, width_, mode, monospace);
            }
        }

        private static int IndexOf(string text, string target, int start, int end)
        {
            var idx = text.IndexOf(target, start);

            if (idx == -1 || idx > end)
            {
                return end;
            }

            return idx;
        }

        private static bool IsWhitespace(string str)
        {
            return _whitespace.IsMatch(str);
        }

        private static List<Metrics> Pre(MSDFFontData fontData, string text, int start, int end, float width, bool monospace)
        {
            var lines = new List<Metrics>();
            var lineStart = start;

            for (var i = start; i < end && i < text.Length; ++i)
            {
                var chr = text[i];
                var isNewline = _newline.IsMatch(chr.ToString());

                if (isNewline || i == end - 1)
                {
                    var lineEnd = isNewline ? i : i + 1;
                    var measured = monospace ? Monospace(text, lineStart, lineEnd, width) : ComputeMetrics(fontData, text, lineStart, lineEnd, width);
                    lines.Add(measured);

                    lineStart = i + 1;
                }
            }

            return lines;
        }

        private static List<Metrics> Greedy(MSDFFontData fontData, string text, int start, int end, float width, WrapMode mode, bool monospace)
        {
            var lines = new List<Metrics>();

            var testWidth = width;

            if (mode == WrapMode.NO_WRAP)
            {
                testWidth = float.MaxValue;
            }

            while (start < end && start < text.Length)
            {
                var newLine = IndexOf(text, _newlineChar, start, end);

                while (start < newLine)
                {
                    if (!IsWhitespace(text[start].ToString()))
                    {
                        break;
                    }

                    ++start;
                }

                var measured = monospace ? Monospace(text, start, newLine, width) : ComputeMetrics(fontData, text, start, newLine, width);

                var lineEnd = start + (measured.end - measured.start);
                var nextStart = lineEnd + _newlineChar.Length;

                if (lineEnd < newLine)
                {
                    while (lineEnd > start)
                    {
                        if (IsWhitespace(text[lineEnd].ToString()))
                        {
                            break;
                        }

                        --lineEnd;
                    }

                    if (lineEnd == start)
                    {
                        if (nextStart > start + _newlineChar.Length)
                        {
                            --nextStart;
                        }

                        lineEnd = nextStart;
                    }
                    else
                    {
                        nextStart = lineEnd;

                        while (lineEnd > start)
                        {
                            if (!IsWhitespace(text[lineEnd - _newlineChar.Length].ToString()))
                            {
                                break;
                            }

                            --lineEnd;
                        }
                    }
                }

                if (lineEnd >= start)
                {
                    var result = monospace ? Monospace(text, start, lineEnd, testWidth) : ComputeMetrics(fontData, text, start, lineEnd, testWidth);
                    lines.Add(result);
                }

                start = nextStart;
            }

            return lines;
        }

        private static Metrics Monospace(string text, int start, int end, float width)
        {
            var glyphs = Mathf.Min((int)width, end - start);

            return new Metrics()
            {
                start = start,
                end = start + glyphs
            };
        }

        private static Metrics ComputeMetrics(MSDFFontData fontData, string text, int start, int end, float width, float? letterSpacing = null)
        {
            var spacing = null != letterSpacing ? letterSpacing : 0f;
            var curPen = 0f;
            var curWidth = 0f;
            var count = 0;

            Glyph glyph = null;
            Glyph lastGlyph = null;

            if (null == fontData || 0 == fontData.CharCount())
            {
                return new Metrics()
                {
                    start = start,
                    end = start,
                    width = 0f
                };
            }

            end = Mathf.Min(text.Length, end);

            for (var i = start; i < end; ++i)
            {
                var id = MSDFFontData.GetCharCode(text[i]);
                glyph = fontData.GetGlyphById(id);

                if (null != glyph)
                {
                    var xoff = glyph.xoffset;
                    var kern = null == lastGlyph ? 0f : fontData.GetKerningAmount(lastGlyph.id, glyph.id);
                  
                    curPen += kern;

                    var nextPen = curPen + glyph.xadvance;
                    var nextWidth = curPen + glyph.width;

                    if (nextWidth >= width || nextPen >= width)
                    {
                        break;
                    }

                    curPen = nextPen;
                    curWidth = nextWidth;
                    lastGlyph = glyph;
                }

                ++count;
            }

            if (null != lastGlyph)
            {
                curWidth += lastGlyph.xoffset;
            }

            return new Metrics()
            {
                start = start,
                end = start + count,
                width = curWidth
            };
        }
    }
}
