using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MSDFText
{
    using MSDFGlyphID = System.Int32;

    public class Glyph
    {
        public MSDFGlyphID id { get; set; }
        public int index { get; set; }
        public string character { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        public float xoffset { get; set; }
        public float yoffset { get; set; }
        public float xadvance { get; set; }
        public int channel { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public int page { get; set; }
    }

    public class Info
    {
        public string face { get; set; }
        public int size { get; set; }
        public int bold { get; set; }
        public int italic { get; set; }
        public List<string> charset { get; set; }
        public int unicode { get; set; }
        public int stretchH { get; set; }
        public int smooth { get; set; }
        public int aa { get; set; }
        public List<float> padding { get; set; }
        public List<float> spacing { get; set; }
    }

    public class CommonData
    {
        public float lineHeight { get; set; }
        public float baseLine { get; set; }
        public float scaleW { get; set; }
        public float scaleH { get; set; }
        public int pages { get; set; }
        public int packed { get; set; }
        public int alphaChnl { get; set; }
        public int redChnl { get; set; }
        public int greenChnl { get; set; }
        public int blueChnl { get; set; }

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            baseLine = (float)_additionalData["base"];
        }
    }

    public class DistanceFieldData
    {
        public string fieldType { get; set; }
        public float distanceRange { get; set; }
    }

    public class Kerning
    {
        public MSDFGlyphID first { get; set; }
        public MSDFGlyphID second { get; set; }
        public float amount { get; set; }
    }

    public class MSDFFontData
    {
        public List<string> pages { get; set; }
        public Info info { get; set; }
        public CommonData common { get; set; }
        public DistanceFieldData distanceField { get; set; }

        private Dictionary<MSDFGlyphID, Glyph> _charData { get; set; }
        private List<Kerning> _kernings { get; set; }

        private readonly List<string> M_WIDTH = new List<string>() { "m", "w" };

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public MSDFFontData()
        {
            _charData = new Dictionary<int, Glyph>();
            _kernings = new List<Kerning>();
            _additionalData = new Dictionary<string, JToken>();
        }

        public bool HasChar(MSDFGlyphID id)
        {
            return _charData.ContainsKey(id);
        }

        public int CharCount()
        {
            return _charData.Count;
        }

        public Glyph GetGlyphById(MSDFGlyphID id)
        {
            if (0 == _charData.Count || !HasChar(id))
            {
                return null;
            }

            return _charData[id];
        }

        public Glyph GetGlyphByIndex(int index)
        {
            return _charData[0];
        }

        public Glyph GetMGlyph()
        {
            foreach (var str in M_WIDTH)
            {
                var id = MSDFFontData.GetCharCode(str);

                if (HasChar(id))
                {
                    return GetGlyphById(id);
                }
            }

            return null;
        }

        public float GetKerningAmount(MSDFGlyphID firstId, MSDFGlyphID secondId)
        {
            var result = _kernings.Where(elem => elem.first == firstId && elem.second == secondId).ToArray();

            if (0 < result.Count())
            {
                return result[0].amount;
            }

            return 0f;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (var key in _additionalData.Keys)
            {
                if ("chars" == key)
                {
                    foreach (var c in _additionalData[key])
                    {
                        _charData.Add((MSDFGlyphID)c["id"], new Glyph()
                        {
                            id = (MSDFGlyphID)c["id"],
                            index = (int)c["index"],
                            character = (string)c["char"],
                            width = (float)c["width"],
                            height = (float)c["height"],
                            xoffset = (float)c["xoffset"],
                            yoffset = (float)c["yoffset"],
                            xadvance = (float)c["xadvance"],
                            channel = (int)c["chnl"],
                            x = (float)c["x"],
                            y = (float)c["y"],
                            page = (int)c["page"]
                        });
                    }
                }
                else if ("kernings" == key)
                {
                    foreach (var kerning in _additionalData[key])
                    {
                        _kernings.Add(new Kerning()
                        {
                            first = (int)kerning["first"],
                            second = (int)kerning["second"],
                            amount = (float)kerning["amount"]
                        });
                    }
                }
            }
        }

        public static int GetCharCode(string str)
        {
            return (int)str[0];
        }

        public static int GetCharCode(char chr)
        {
            return (int)chr.ToString()[0];
        }
    }
}
