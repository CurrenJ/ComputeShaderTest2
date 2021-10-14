using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using ComputeShaderUtility;
using System.IO;
using UnityEditor;

public class Controller : MonoBehaviour
{
    // Shader Properties, Buffers, Textures
    public ComputeShader computeShader;
    ComputeBuffer agentBuffer;
    ComputeBuffer teamSettingsBuffer;
    public Vector2 targetResolution; //ref 1920x1080
    private Vector2 size;

    // Tex
    [SerializeField, HideInInspector] protected RenderTexture displayTexture;
    [SerializeField, HideInInspector] protected RenderTexture trailMap;
    [SerializeField, HideInInspector] protected RenderTexture diffusedTrailMap;
    [SerializeField, HideInInspector] protected RenderTexture obstructionMap;
    [SerializeField, HideInInspector] protected RenderTexture playerMap;

    // Simulation Properties
    public int population;
    [Range(1, 3)]
    public int teams;
    public enum SpawnMode { Random, Point, InwardCircle, RandomCircle, Homes }
    public SpawnMode spawnMode;
    public float decayFactor;
    public float diffuseFactor;
    public bool generateRandomObstacles;
    [SerializeField]
    private TeamSettings[] teamSettings = new TeamSettings[] { new TeamSettings() { }, new TeamSettings() { }, new TeamSettings() { } };


    // Playback Settings
    public int stepsPerUpdate;
    public bool showAgentsOnly;
    public bool showObstructionsOnly;
    public bool useRawColorChannels;
    public bool useHsvForColorRemapping;

    // Color Fields

    public Color teamAColorTarget;
    public Color teamBColorTarget;
    public Color teamCColorTarget;
    public Color bgColorTarget;
    public Color bgColor;
    public bool useCurrenPastelPack;
    public PaletteModule[] paletteModules;

    // Render Settings
    public bool renderToPng;
    public bool saveRawChannelsSeparately;
    public string path;
    public int frame;
    public int step;
    public float renderTimeScale;
    public float framesPerSecond;
    public int frameLimit;

    // Other
    private bool initialized;
    public Vector3[] homes;

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

        for (int n = 0; n < 10; n++)
        {
            if (Input.GetKeyDown((KeyCode)n + ((int)KeyCode.Alpha1)))
                if (Input.GetKeyDown((KeyCode)n + ((int)KeyCode.Alpha1)))
                {
                    if (n < paletteModules[0].palettes.Length)
                    {
                        SelectPalette(paletteModules[0].palettes[n]);
                    }
                }
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

        // Update colors
        teamSettings[0].teamColor = Color.Lerp(teamSettings[0].teamColor, teamAColorTarget, Time.deltaTime * 0.5f);
        teamSettings[1].teamColor = Color.Lerp(teamSettings[1].teamColor, teamBColorTarget, Time.deltaTime * 0.5f);
        teamSettings[2].teamColor = Color.Lerp(teamSettings[2].teamColor, teamCColorTarget, Time.deltaTime * 0.5f);
        bgColor = Color.Lerp(bgColor, bgColorTarget, Time.deltaTime * 0.5f);
    }

    void Update()
    {
        if (renderToPng)
            RenderToPng();
    }

    private void Init()
    {
        ComputeHelper.Release(agentBuffer);
        ComputeHelper.Release(teamSettingsBuffer);

        size = new Vector2(targetResolution.x, targetResolution.y);
        if (RuntimePlatform.Android == Application.platform)
            size = new Vector2(Screen.width / 4, Screen.height / 4);
        paletteModules = this.gameObject.GetComponents<PaletteModule>();
        CreateRenderTexture(ref displayTexture, (int)size.x, (int)size.y);
        CreateRenderTexture(ref trailMap, (int)size.x, (int)size.y);
        CreateRenderTexture(ref diffusedTrailMap, (int)size.x, (int)size.y);
        CreateRenderTexture(ref obstructionMap, (int)size.x, (int)size.y);
        CreateRenderTexture(ref playerMap, (int)size.x, (int)size.y);
        computeShader.SetTexture(3, "ObstructionMap", obstructionMap);
        if (generateRandomObstacles)
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

            ComputeHelper.CreateAndSetBuffer<TeamSettings>(ref teamSettingsBuffer, teamSettings, computeShader, "teamSettings", 0);
            ComputeHelper.CreateAndSetBuffer<TeamSettings>(ref teamSettingsBuffer, teamSettings, computeShader, "teamSettings", 2);

            computeShader.SetBool("useHsvForColorRemapping", useHsvForColorRemapping);

            computeShader.SetVector("bgColor", bgColor);
            ComputeHelper.Dispatch(computeShader, population, 1, 1, 0);
            computeShader.Dispatch(1, Screen.width / 8, Screen.height / 8, 1);
            ComputeHelper.CopyRenderTexture(diffusedTrailMap, trailMap);
            if (!useRawColorChannels)
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
                computeShader.SetInts("redHome", new int[] { (int)(size.x / 2 + homes[0].x * size.x), (int)(size.y / 2 + homes[0].y * size.y), (int)homes[0].z });
                computeShader.SetInts("greenHome", new int[] { (int)(size.x / 2 + homes[1].x * size.x), (int)(size.y / 2 + homes[1].y * size.y), (int)homes[1].z });
                computeShader.SetInts("blueHome", new int[] { (int)(size.x / 2 + homes[2].x * size.x), (int)(size.y / 2 + homes[2].y * size.y), (int)homes[2].z });
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
                computeShader.SetInts("redHome", new int[] { (int)(size.x / 2), (int)(size.y / 2), (int)homes[0].z });
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
                startPos = centre + Random.insideUnitCircle * size.y * 0.5f;
                angle = randomAngle;
            }



            agents[i] = new Agent() { position = startPos, angle = angle, speciesMaskAndTrailTypes = species, unusedSpeciesChannel = 0 };
        }


        ComputeHelper.CreateAndSetBuffer<Agent>(ref agentBuffer, agents, computeShader, "agents", 0);
        computeShader.SetInt("numAgents", population);
    }

    private void RenderToScreen(RenderTexture destination)
    {
        if (!renderToPng)
        {
            if (initialized)
                ComputeHelper.ClearRenderTexture(playerMap);

            // We leave this at 1 because when live-rendering, adjusting steps per update doesn't actually change the speed of simulation, because it is bound by performance.
            UpdateSim(1);

            if (showAgentsOnly)
                Graphics.Blit(playerMap, destination);
            else if (showObstructionsOnly)
                Graphics.Blit(obstructionMap, destination);
            else if (useRawColorChannels)
                Graphics.Blit(trailMap, destination);
            else Graphics.Blit(displayTexture, destination);
        }
    }

    private void RenderToPng()
    {
        if (frameLimit <= 0 || frame < frameLimit)
        {
            if (initialized)
                ComputeHelper.ClearRenderTexture(playerMap);

            UpdateSim(stepsPerUpdate);

            if (renderToPng)
            {
                if (saveRawChannelsSeparately || useRawColorChannels)
                    DumpRenderTexture(ref trailMap, Path.Combine(Application.streamingAssetsPath, path, "raw/"), frame + "_rgb.png");
                DumpRenderTexture(ref displayTexture, Path.Combine(Application.streamingAssetsPath, path), frame + ".png");
                framesPerSecond = frame / Time.time;
            }
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!renderToPng)
            RenderToScreen(destination);
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

    private void UpdateSim(int steps)
    {
        for (int s = 0; s < steps; s++)
        {
            computeShader.SetFloat("deltaTime", renderTimeScale);
            computeShader.SetFloat("time", steps * renderTimeScale);

            SetShaderParameters();
        }
        if (renderToPng && (frameLimit <= 0 || frame < frameLimit))
            frame++;
        step += steps;
    }

    public void randomizeSettings(bool changeSettings = true, bool changeColor = false, bool reinitialize = false)
    {
        if (changeSettings)
        {
            spawnMode = (SpawnMode)Mathf.FloorToInt(Random.value * 5);
            decayFactor = 0.1f * Mathf.Pow(10, -1 * Mathf.FloorToInt(Random.value * 7));
            diffuseFactor = 1.0f * Mathf.Pow(10, -1 * Mathf.FloorToInt(Random.value * 4));

            for (int t = 0; t < teamSettings.Length; t++)
            {
                TeamSettings ts = teamSettings[t];
                ts.moveSpeed = UnityEngine.Random.Range(0.75f, 1.25f);
                ts.turnFactor = UnityEngine.Random.Range(0.75f, 1f);
                ts.sensorPosOffset = Mathf.FloorToInt(Random.value * 25 + 5);
                ts.sensorAngleOffset = UnityEngine.Random.Range(15f, 45f);
                teamSettings[t] = ts;
            }

            teams = Mathf.FloorToInt(Random.value * 3) + 1;

            generateRandomObstacles = false; //Mathf.FloorToInt(Random.value * 2.0f) != 0;
        }
        if (changeColor)
        {
            paletteModules = this.gameObject.GetComponents<PaletteModule>();
            if (paletteModules != null && paletteModules.Length > 0)
            {
                if (useCurrenPastelPack)
                {
                    PaletteModule palettes = paletteModules[Mathf.FloorToInt(Random.value * paletteModules.Length)];
                    PaletteModule.Palette palette = palettes.palettes[Mathf.FloorToInt(Random.value * palettes.palettes.Length)];
                    SelectPalette(palette);
                }
                else
                {
                    teamAColorTarget = Random.ColorHSV();
                    teamBColorTarget = Random.ColorHSV();
                    teamCColorTarget = Random.ColorHSV();
                    bgColorTarget = Random.ColorHSV() * 0.15f;
                }
            }

        }
        if (reinitialize)
            Init();
    }

    private void SelectPalette(PaletteModule.Palette palette)
    {
        teamAColorTarget = palette.palette[0];
        teamBColorTarget = palette.palette[1];
        teamCColorTarget = palette.palette[2];
        bgColorTarget = palette.palette[3];
    }

    void OnDestroy()
    {
        ComputeHelper.Release(agentBuffer);
        ComputeHelper.Release(teamSettingsBuffer);
    }

    public struct Agent
    {
        public Vector2 position;
        public float angle;
        public Vector3Int speciesMaskAndTrailTypes;
        public int unusedSpeciesChannel;
    }

    [System.Serializable]
    public struct TeamSettings
    {
        public float turnFactor;
        public float moveSpeed;
        public float sensorAngleOffset;
        public float sensorPosOffset;
        public float sensorRadius;
        public Color teamColor;
    }

    public static void DumpRenderTexture(ref RenderTexture rt, string path, string filename)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes;
        bytes = tex.EncodeToPNG();
        Destroy(tex);

        System.IO.Directory.CreateDirectory(path);
        System.IO.File.WriteAllBytes(Path.Combine(path, filename), bytes);
    }
}
