using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainSceneController : MonoBehaviour
{
    struct TransformData
    {
        public Vector3 translate;
        public Vector3 scale;
        public Vector3 rotation;
        public int enable;

        public TransformData(Vector3 translate, Vector3 scale, Vector3 rotation, int enable)
        {
            this.translate = translate;
            this.scale = scale;
            this.rotation = rotation;
            this.enable = enable;
        }
    };

    [SerializeField, Range(1, 100000)] public int instanceCount = 1000;
    [SerializeField, Range(1, 20)] public int maxTextLength = 20;
    [SerializeField, Range(1, 20)] public int currentTextLength = 20;

    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;
    public Texture2D texture;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private int cachedCurrentTextLength = 20;
    private ComputeBuffer transformDataBuffer;
    private ComputeBuffer colorBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    void Start()
    {
        instanceMesh = CreateMesh();

        instanceMaterial = new Material(Shader.Find("Unlit/InstanceShader"));
        instanceMaterial.SetColor("_Color", Random.ColorHSV());
        instanceMaterial.SetTexture("_MainTex", texture);

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

        transformDataBuffer = new ComputeBuffer(instanceCount, 40);
        colorBuffer = new ComputeBuffer(instanceCount, 16);

        TransformData[] transformData = new TransformData[instanceCount];
        Color[] colors = new Color[instanceCount];

        Vector3 center = new Vector3(Random.Range(-100f, 100f), Random.Range(-100f, 100f), Random.Range(-100f, 100f));
        int offset = 0;

        for (int i = 0; i < instanceCount; i++)
        {
            transformData[i] = new TransformData(
                center + new Vector3(offset, 0f, offset * 0.0001f),
                new Vector3(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), 1f),
                new Vector3(Random.Range(-Mathf.PI, Mathf.PI), 0f, 0f),
                offset < currentTextLength ? 1 : 0);
            
            colors[i] = Random.ColorHSV();

            ++offset;

            if (i % maxTextLength == 0)
            {
                offset = 0;
                center.Set(Random.Range(-100f, 100f), Random.Range(-100f, 100f), Random.Range(-100f, 100f));
            }
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
        TransformData[] transformData = new TransformData[instanceCount];
        transformDataBuffer.GetData(transformData);

        int offset = 0;

        for (int i = 0; i < instanceCount; i++)
        {
            transformData[i].enable = offset < currentTextLength ? 1 : 0;

            ++offset;

            if (i % maxTextLength == 0)
            {
                offset = 0;
            }
        }

        transformDataBuffer.SetData(transformData);

        cachedCurrentTextLength = currentTextLength;
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

        var length = 1f;

        var vertices = new List<Vector3>
        {
            new Vector3 (length, length, 0),
            new Vector3 (-length, length, 0),
            new Vector3 (length, -length, 0),
            new Vector3 (-length, -length, 0),
        };

        var triangles = new List<int>
        {
            1, 0, 2,
            1, 2, 3
        };

        var uvs = new List<Vector2>
        {
            new Vector2(1, 1),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(0, 0),
        };

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);

        return mesh;
    }
}
