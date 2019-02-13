using MSDFText;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class MainSceneController : MonoBehaviour
{
    struct TransformData
    {
        public Vector3 translate;
        public Vector3 scale;
        public Vector3 rotation;
        public Vector2 uvOffset;
        public Vector2 uvScale;
        public int enable;

        public TransformData(Vector3 translate, Vector3 scale, Vector3 rotation, Vector2 uvOffset, Vector2 uvScale, int enable)
        {
            this.translate = translate;
            this.scale = scale;
            this.rotation = rotation;
            this.uvOffset = uvOffset;
            this.uvScale = uvScale;
            this.enable = enable;
        }
    };

    struct TextSettings
    {
        public string text;
        public MeshInfo meshInfo;

        public TextSettings(string text, MeshInfo meshInfo)
        {
            this.text = text;
            this.meshInfo = meshInfo;
        }
    };

    private static readonly int MAX_TEXT_LENGTH = 20;

    private List<string> quotes = new List<string>()
    {
        "船に乗りたーいyo～！"
    };

    [SerializeField, Range(1, 100000)] public int instanceCount = 100;
    [SerializeField, Range(1, 20)] public int maxTextLength = 20;
    [SerializeField, Range(1, 20)] public int currentTextLength = 20;
    [SerializeField] private Texture2D msdfTexture;

    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;
    public Texture2D texture;

    private string fontDataPath = "MSDFFonts/NotoSansCJKjp-Regular-japanese-full.json";
    private MSDFFontData fontData;
    private TextSettings[] textSettings;

    private int numTextGroups;
    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private int cachedCurrentTextLength = 20;
    private ComputeBuffer transformDataBuffer;
    private ComputeBuffer colorBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    void Start()
    {
        var path = Path.Combine(Application.dataPath, fontDataPath);

        if (File.Exists(path))
        {
            var source = File.ReadAllText(path);

            fontData = JsonConvert.DeserializeObject<MSDFFontData>(source);

            MeshInfo info = MSDFTextMesh.GetMeshInfo("hello", fontData, true);
            Debug.Log(info.uvs.Length);
        }

        //numTextGroups = GetTextGroupCount();

        textSettings = new TextSettings[1];

        var text = "";

        for (var i = 0; i < textSettings.Length; ++i)
        {
            text = quotes[Random.Range(0, quotes.Count)];
            textSettings[i] = new TextSettings(text, MSDFTextMesh.GetMeshInfo(text, fontData, true));
        }

        instanceMesh = CreateMesh();

        instanceMaterial = new Material(Shader.Find("Unlit/InstanceShader"));
        instanceMaterial.SetColor("_Color", Random.ColorHSV());
        instanceMaterial.SetTexture("_MainTex", msdfTexture);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        UpdateBuffers();
    }

    void Update()
    {
        // Update starting position buffer
        if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
        {
            UpdateBuffers();
        }

        if (cachedCurrentTextLength != currentTextLength)
        {
            UpdateTextLength();
        }

        // Pad input
        if (Input.GetAxisRaw("Horizontal") != 0.0f)
        {
            instanceCount = (int)Mathf.Clamp(instanceCount + Input.GetAxis("Horizontal") * 40000, 1.0f, 100000.0f);
        }
        
        // Render
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(265, 25, 200, 30), "Instance Count: " + instanceCount.ToString());
        instanceCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)instanceCount, 1.0f, 100000.0f);

        GUI.Label(new Rect(265, 50, 200, 30), "Max Text Length: " + currentTextLength.ToString());
        currentTextLength = (int)GUI.HorizontalSlider(new Rect(25, 50, 200, 30), (float)currentTextLength, 1.0f, 20.0f);
    }

    void UpdateBuffers()
    {
        // Ensure submesh index is in range
        if (instanceMesh != null)
        {
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);
        }

        // Transform Matrix
        if (transformDataBuffer != null)
        {
            transformDataBuffer.Release();
        }

        // Colors
        if (colorBuffer != null)
        {
            colorBuffer.Release();
        }

        numTextGroups = GetTextGroupCount();

        var oldLength = textSettings.Length;
        Array.Resize(ref textSettings, numTextGroups);

        var text = "";
        
        if (oldLength < textSettings.Length)
        {
            for (var i = oldLength; i < textSettings.Length; ++i)
            {
                text = quotes[Random.Range(0, quotes.Count)];
                textSettings[i] = new TextSettings(text, MSDFTextMesh.GetMeshInfo(text, fontData, true));

                for (var j = 0; j < textSettings[i].meshInfo.uvs.Length; ++j)
                {
                    Debug.Log($"text \"{text}\" uvs {i}-{j} : x = {textSettings[i].meshInfo.uvs[j].x * 4096f}, y = {textSettings[i].meshInfo.uvs[j].y * 4096f}");
                }
            }
        }

        transformDataBuffer = new ComputeBuffer(instanceCount, 56);
        colorBuffer = new ComputeBuffer(instanceCount, 16);

        TransformData[] transformData = new TransformData[instanceCount];
        Color[] colors = new Color[instanceCount];

        Vector3 center = new Vector3(Random.Range(-100f, 100f), Random.Range(-100f, 100f), Random.Range(-100f, 100f));

        for (var i = 0; i < numTextGroups; ++i)
        {
            var uvIndex = 0;

            for (int j = 0; j < MAX_TEXT_LENGTH; ++j)
            {
                var index = (i * MAX_TEXT_LENGTH) + j;

                var max = textSettings[i].meshInfo.uvs[uvIndex + 3];
                var min = textSettings[i].meshInfo.uvs[uvIndex + 1];
                var uvScale = max - min;
                var uvOffset = min;

                transformData[index] = new TransformData(
                    center + new Vector3(j * 5f, 0f, j * 0.0001f),
                    //new Vector3(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), 1f),
                    new Vector3(uvScale.x * 32f, uvScale.y * 32f, 1f),
                    new Vector3(Random.Range(-Mathf.PI, Mathf.PI), 0f, 0f),
                    //new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)),
                    uvOffset,
                    uvScale,
                    j < textSettings[i].text.Length ? 1 : 0);

                colors[index] = Random.ColorHSV();

                uvIndex = Mathf.Min(uvIndex + 4, textSettings[i].meshInfo.uvs.Length - 4);
            }

            center.Set(Random.Range(-100f, 100f), Random.Range(-100f, 100f), Random.Range(-100f, 100f));
        }
        

        transformDataBuffer.SetData(transformData);
        colorBuffer.SetData(colors);

        instanceMaterial.SetBuffer("transformDataBuffer", transformDataBuffer);
        instanceMaterial.SetBuffer("colorBuffer", colorBuffer);

        // Indirect args
        if (instanceMesh != null)
        {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }

        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    void UpdateTextLength()
    {
        return;
        TransformData[] transformData = new TransformData[instanceCount];
        transformDataBuffer.GetData(transformData);

        var offset = 0;
        var length = Random.Range(1, currentTextLength);

        for (int i = 0; i < instanceCount; i++)
        {
            transformData[i].enable = offset < length ? 1 : 0;

            ++offset;

            if (i % maxTextLength == 0)
            {
                offset = 0;
                length = Random.Range(1, currentTextLength);
            }
        }

        transformDataBuffer.SetData(transformData);

        cachedCurrentTextLength = currentTextLength;
    }

    int GetTextGroupCount()
    {
        return instanceCount / MAX_TEXT_LENGTH;
    }

    void OnDisable()
    {
        if (transformDataBuffer != null)
        {
            transformDataBuffer.Release();
        }

        transformDataBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }

        argsBuffer = null;
    }

    Mesh CreateMesh()
    {
        var mesh = new Mesh();

        mesh.name = "TestMesh";

        var length = 10f;

        var vertices = new List<Vector3>
        {
            new Vector3 (-length, length, 0),
            new Vector3 (-length, -length, 0),
            new Vector3 (length, -length, 0),
            new Vector3 (length, length, 0),
        };

        var triangles = new List<int>
        {
            0, 3, 1,
            1, 3, 2
        };

        var uvs = new List<Vector2>
        {
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            
        };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        return mesh;
    }
}
