using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using ComputeShaderUtility;

public class Controller : MonoBehaviour
{
    public ComputeShader computeShader;
    ComputeBuffer agentBuffer;
    public Vector2 referenceResolution; //ref 1920x1080
    public Vector2 targetResolution; //ref 1920x1080
    private Vector2 size;
    public int population;
    [SerializeField, HideInInspector] protected RenderTexture displayTexture;
    [SerializeField, HideInInspector] protected RenderTexture trailMap;
    [SerializeField, HideInInspector] protected RenderTexture diffusedTrailMap;
    [SerializeField, HideInInspector] protected RenderTexture obstructionMap;
    [SerializeField, HideInInspector] protected RenderTexture playerMap;
    public enum SpawnMode { Random, Point, InwardCircle, RandomCircle, Homes }
    public SpawnMode spawnMode;
    public float turnFactor;
    public float decayFactor;
    public float diffuseFactor;
    [Range(1.0f, 16.0f)]
    public float moveSpeed;
    private bool initialized;
    public bool showAgentsOnly;
    public bool showObstructionsOnly;
    [Range(1, 3)]
    public int teams;
    public Color teamAColorTarget;
    public Color teamBColorTarget;
    public Color teamCColorTarget;
    public Color bgColorTarget;

    public Color teamAColor;
    public Color teamBColor;
    public Color teamCColor;
    public Color bgColor;
    public Vector3[] homes;
    public bool useCurrenPastelPack;
    public Color[] pastelPackLights;
    public Color[] pastelPackMids;
    public Color[] pastelPackHighs;
    public Color[] pastelBackBgs;

    public float screenScalingFactor;

    protected virtual void Start()
    {
        Debug.Log(SystemInfo.graphicsDeviceName);
        Init();
    }

    void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            randomizeSettings(true, false, false);
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            randomizeSettings(false, true, false);
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            randomizeSettings(true, true, true);
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 screenPosition = Input.GetTouch(0).position;
            if (screenPosition.x / Screen.width < 0.5 && screenPosition.y / Screen.height > 0.5)
                randomizeSettings(true, false, false);
            else if (screenPosition.x / Screen.width >= 0.5 && screenPosition.y / Screen.height > 0.5)
                randomizeSettings(false, true, false);
            else if (screenPosition.y / Screen.height < 0.5)
                randomizeSettings(true, true, true);
        }

        teamAColor = Color.Lerp(teamAColor, teamAColorTarget, Time.deltaTime * 0.5f);
        teamBColor = Color.Lerp(teamBColor, teamBColorTarget, Time.deltaTime * 0.5f);
        teamCColor = Color.Lerp(teamCColor, teamCColorTarget, Time.deltaTime * 0.5f);
        bgColor = Color.Lerp(bgColor, bgColorTarget, Time.deltaTime * 0.5f);
    }

    private void Init()
    {
        ComputeHelper.Release(agentBuffer);
        if (Screen.width > Screen.height)
            screenScalingFactor = Screen.width / referenceResolution.x * (referenceResolution.x / targetResolution.x);
        else
            screenScalingFactor = Screen.height / referenceResolution.y * (referenceResolution.y / targetResolution.y);

        size = new Vector2(Screen.width, Screen.height);
        size /= screenScalingFactor;
        CreateRenderTexture(ref displayTexture, (int)size.x, (int)size.y);
        CreateRenderTexture(ref trailMap, (int)size.x, (int)size.y);
        CreateRenderTexture(ref diffusedTrailMap, (int)size.x, (int)size.y);
        CreateRenderTexture(ref obstructionMap, (int)size.x, (int)size.y);
        CreateRenderTexture(ref playerMap, (int)size.x, (int)size.y);
        computeShader.SetTexture(3, "ObstructionMap", obstructionMap);
        computeShader.Dispatch(3, Screen.width / 8, Screen.height / 8, 1);
        CreateAgents();
        initialized = true;
    }

    private void SetShaderParameters()
    {
        if (initialized)
        {
            computeShader.SetTexture(0, "TrailMap", trailMap);
            computeShader.SetTexture(0, "ObstructionMap", obstructionMap);
            computeShader.SetTexture(0, "PlayerMap", playerMap);

            computeShader.SetTexture(1, "DiffusedTrailMap", diffusedTrailMap);
            computeShader.SetTexture(1, "TrailMap", trailMap);

            computeShader.SetTexture(2, "TrailMap", trailMap);
            computeShader.SetTexture(2, "Result", displayTexture);
            computeShader.SetInt(Shader.PropertyToID("width"), (int)size.x);
            computeShader.SetInt(Shader.PropertyToID("height"), (int)size.y);
            computeShader.SetFloat("decayRate", decayFactor);
            computeShader.SetFloat("diffuseRate", diffuseFactor);
            computeShader.SetFloat("turnFactor", turnFactor);
            computeShader.SetFloat("moveSpeed", moveSpeed);

            computeShader.SetInts("redHome", new int[] { (int)(size.x / 2 + homes[0].x * size.x), (int)(size.y / 2 + homes[0].y * size.y), (int)homes[0].z });
            computeShader.SetInts("greenHome", new int[] { (int)(size.x / 2 + homes[1].x * size.x), (int)(size.y / 2 + homes[1].y * size.y), (int)homes[1].z });
            computeShader.SetInts("blueHome", new int[] { (int)(size.x / 2 + homes[2].x * size.x), (int)(size.y / 2 + homes[2].y * size.y), (int)homes[2].z });

            computeShader.SetFloats("teamColorA", new float[] { teamAColor.r, teamAColor.g, teamAColor.b });
            computeShader.SetFloats("teamColorB", new float[] { teamBColor.r, teamBColor.g, teamBColor.b });
            computeShader.SetFloats("teamColorC", new float[] { teamCColor.r, teamCColor.g, teamCColor.b });
            computeShader.SetVector("bgColor", bgColor);

            ComputeHelper.Dispatch(computeShader, population, 1, 1, 0);
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
            Vector3Int species = new Vector3Int(0, 0, 0);
            float seed = Random.value;
            if (teams == 3)
            {
                if (seed < 0.33f)
                {
                    species.x = 1;
                    if (spawnMode == SpawnMode.Homes)
                        centre = new Vector2((int)(size.x / 2 + homes[0].x * size.x), (int)(size.y / 2 + homes[0].y * size.y));
                }
                else if (seed < 0.66f)
                {
                    species.y = 1;
                    if (spawnMode == SpawnMode.Homes)
                        centre = new Vector2((int)(size.x / 2 + homes[1].x * size.x), (int)(size.y / 2 + homes[1].y * size.y));
                }
                else
                {
                    species.z = 1;
                    if (spawnMode == SpawnMode.Homes)
                        centre = new Vector2((int)(size.x / 2 + homes[2].x * size.x), (int)(size.y / 2 + homes[2].y * size.y));
                }
            }
            else if (teams == 2)
            {
                if (seed < 0.5f)
                {
                    species.x = 1;
                    if (spawnMode == SpawnMode.Homes)
                        centre = new Vector2((int)(size.x / 2 + homes[0].x * size.x), (int)(size.y / 2 + homes[0].y * size.y));
                }
                else if (seed >= 0.5f)
                {
                    species.y = 1;
                    if (spawnMode == SpawnMode.Homes)
                        centre = new Vector2((int)(size.x / 2 + homes[1].x * size.x), (int)(size.y / 2 + homes[1].y * size.y));
                }
            }
            else
            {
                species.x = 1;
                if (spawnMode == SpawnMode.Homes)
                    centre = new Vector2((int)(size.x / 2 + homes[2].x * size.x), (int)(size.y / 2 + homes[2].y * size.y));
            }


            Vector2 startPos = new Vector2(Random.value * size.x, Random.value * size.y);
            float randomAngle = Random.value * Mathf.PI * 2;
            float angle = Mathf.Atan2(centre.y - startPos.y, centre.x - startPos.x);

            if (spawnMode == SpawnMode.Point)
            {
                startPos = centre;
                angle = randomAngle;
            }
            else if (spawnMode == SpawnMode.Homes)
            {
                startPos = centre;
                angle = randomAngle;
            }
            else if (spawnMode == SpawnMode.Random)
            {
                startPos = new Vector2(Random.Range(0, size.x), Random.Range(0, size.y));
                angle = randomAngle;
            }
            else if (spawnMode == SpawnMode.InwardCircle)
            {
                startPos = centre + Random.insideUnitCircle * size.y * 0.5f;
                angle = Mathf.Atan2((centre - startPos).normalized.y, (centre - startPos).normalized.x);
            }
            else if (spawnMode == SpawnMode.RandomCircle)
            {
                startPos = centre + Random.insideUnitCircle * size.y * 0.15f;
                angle = randomAngle;
            }



            agents[i] = new Agent() { position = startPos, angle = angle, speciesMaskAndTrailTypes = species, unusedSpeciesChannel = 0 };
        }


        ComputeHelper.CreateAndSetBuffer<Agent>(ref agentBuffer, agents, computeShader, "agents", 0);
        computeShader.SetInt("numAgents", population);
    }

    private void Render(RenderTexture destination)
    {
        if (initialized)
            ComputeHelper.ClearRenderTexture(playerMap);

        UpdateSim();
        SetShaderParameters();

        if (showAgentsOnly)
            Graphics.Blit(playerMap, destination);
        else if (showObstructionsOnly)
            Graphics.Blit(obstructionMap, destination);
        else Graphics.Blit(displayTexture, destination);
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
        if (texture != null)
            ComputeHelper.ClearRenderTexture(texture);
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

    public void randomizeSettings(bool changeSettings = true, bool changeColor = false, bool reinitialize = false)
    {
        if (changeSettings)
        {
            spawnMode = (SpawnMode)Mathf.FloorToInt(Random.value * 5);
            turnFactor = Random.value;
            decayFactor = 0.1f * Mathf.Pow(10, -1 * Mathf.FloorToInt(Random.value * 8));
            diffuseFactor =  1.0f * Mathf.Pow(10, -1 * Mathf.FloorToInt(Random.value * 4));
            moveSpeed = Random.value * 5 + 1;
            teams = Mathf.FloorToInt(Random.value * 3) + 1;
        }
        if (changeColor)
        {
            if (useCurrenPastelPack)
            {
                int index = Mathf.FloorToInt(Random.value * pastelPackLights.Length);//pastelPackLights.Length-1;//
                teamAColorTarget = pastelPackLights[index];
                teamBColorTarget = pastelPackMids[index];
                teamCColorTarget = pastelPackHighs[index];
                bgColorTarget = pastelBackBgs[index];
            }
            else
            {
                teamAColorTarget = Random.ColorHSV();
                teamBColorTarget = Random.ColorHSV();
                teamCColorTarget = Random.ColorHSV();
                bgColorTarget = Random.ColorHSV() * 0.15f;
            }
        }
        if (reinitialize)
            Init();
    }

    void OnDestroy()
    {
        ComputeHelper.Release(agentBuffer);
    }

    public struct Agent
    {
        public Vector2 position;
        public float angle;
        public Vector3Int speciesMaskAndTrailTypes;
        public int unusedSpeciesChannel;
    }


}
