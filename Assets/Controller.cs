using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using ComputeShaderUtility;

public class Controller : MonoBehaviour
{
    public ComputeShader computeShader;
    ComputeBuffer agentBuffer;
    public Vector2 size;
    public int population;
    [SerializeField, HideInInspector] protected RenderTexture displayTexture;
    [SerializeField, HideInInspector] protected RenderTexture trailMap;
    [SerializeField, HideInInspector] protected RenderTexture diffusedTrailMap;
    private bool initialized;

    protected virtual void Start()
    {
        Init();
    }

    private void Init()
    {
        CreateRenderTexture(ref displayTexture, (int)size.x, (int)size.y);
        CreateRenderTexture(ref trailMap, (int)size.x, (int)size.y);
        CreateRenderTexture(ref diffusedTrailMap, (int)size.x, (int)size.y);
        CreateAgents();
        initialized = true;
    }

    private void SetShaderParameters()
    {
        if (initialized)
        {
            computeShader.SetTexture(0, "TrailMap", trailMap);
            computeShader.SetTexture(1, "DiffusedTrailMap", diffusedTrailMap);
            computeShader.SetTexture(1, "TrailMap", trailMap);
            computeShader.SetTexture(2, "TrailMap", trailMap);
            computeShader.SetTexture(2, "Result", displayTexture);
            computeShader.SetInt(Shader.PropertyToID("width"), (int)size.x);
            computeShader.SetInt(Shader.PropertyToID("height"), (int)size.y);
            computeShader.SetFloat("decayRate", 0.00001f);
            computeShader.SetFloat("diffuseRate", 1f);
            computeShader.Dispatch(0, population, 1, 1);
            computeShader.Dispatch(1, Screen.width / 8, Screen.height / 8, 1);
            ComputeHelper.CopyRenderTexture(diffusedTrailMap, trailMap);
            computeShader.Dispatch(2, Screen.width / 8, Screen.height / 8, 1);
        }
    }

    private void CreateAgents()
    {
        // Create agents with initial positions and angles
        Agent[] agents = new Agent[population];
        for (int i = 0; i < agents.Length; i++)
        {
            Vector2 centre = new Vector2(size.x / 2, size.y / 2);
            Vector2 startPos = new Vector2(Random.value * size.x, Random.value * size.y);
            float randomAngle = Random.value * Mathf.PI * 2;
            float angle = Mathf.Atan2(centre.y - startPos.y, centre.x - startPos.x);

            startPos = centre + Random.insideUnitCircle * size.y * 0.5f;
            angle = Mathf.Atan2((centre - startPos).normalized.y, (centre - startPos).normalized.x);

            agents[i] = new Agent() { position = startPos, angle = angle };
        }


        ComputeHelper.CreateAndSetBuffer<Agent>(ref agentBuffer, agents, computeShader, "agents", 0);
        computeShader.SetInt("numAgents", population);
    }

    private void Render(RenderTexture destination)
    {
        UpdateSim();
        SetShaderParameters();

        // Graphics.Blit(trailMap, destination);
        Graphics.Blit(displayTexture, destination);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Render(destination);
    }


    public static void CreateRenderTexture(ref RenderTexture texture, int width, int height)
    {
        CreateRenderTexture(ref texture, width, height, FilterMode.Point, GraphicsFormat.R16G16B16A16_SFloat);
    }


    public static void CreateRenderTexture(ref RenderTexture texture, int width, int height, FilterMode filterMode, GraphicsFormat format)
    {
        if (texture == null || !texture.IsCreated() || texture.width != width || texture.height != height || texture.graphicsFormat != format)
        {
            if (texture != null)
            {
                texture.Release();
            }
            texture = new RenderTexture(width, height, 0);
            texture.graphicsFormat = format;
            texture.enableRandomWrite = true;

            texture.autoGenerateMips = false;
            texture.Create();
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = filterMode;
    }

    private void UpdateSim()
    {
        computeShader.SetFloat("deltaTime", Time.fixedDeltaTime);
        computeShader.SetFloat("time", Time.time);
    }

    public struct Agent
    {
        public Vector2 position;
        public float angle;
    }
}
