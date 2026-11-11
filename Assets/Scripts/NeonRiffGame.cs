using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedurally builds and runs the Neon Riff rhythm-game demo.</summary>
public sealed class NeonRiffGame : MonoBehaviour
{
    private enum GameState { Menu, Playing, Paused, Results }
    private enum Difficulty { Chill, Voltage, Overdrive }

    private sealed class ChartNote
    {
        public int lane;
        public float time;
        public GameObject visual;
        public bool resolved;
    }

    private static readonly Color[] LaneColors =
    {
        new Color(0.10f, 0.95f, 0.92f),
        new Color(0.56f, 0.35f, 1.00f),
        new Color(1.00f, 0.25f, 0.48f),
        new Color(1.00f, 0.72f, 0.18f)
    };
    private static readonly KeyCode[] LaneKeys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F };
    private const float LaneSpacing = 1.65f;
    private const float HitZ = 2.0f;
    private const float TravelTime = 2.15f;
    private const float PerfectWindow = 0.085f;
    private const float GoodWindow = 0.17f;
    private const float MissWindow = 0.22f;
    private const float SongLength = 32f;

    private readonly List<ChartNote> chart = new List<ChartNote>();
    private readonly List<GameObject> sceneObjects = new List<GameObject>();
    private GameState state = GameState.Menu;
    private Difficulty difficulty = Difficulty.Voltage;
    private Camera gameCamera;
    private AudioSource musicSource;
    private Material[] laneMaterials;
    private Material[] noteMaterials;
    private Material darkMaterial;
    private Material railMaterial;
    private Material hitMaterial;
    private GUIStyle titleStyle, headingStyle, bodyStyle, smallStyle, buttonStyle, scoreStyle;
    private bool stylesReady;
    private float songTime, pauseTime, lastJudgementTime;
    private string judgement = "READY";
    private int score, combo, bestCombo, health = 100, perfectCount, goodCount, missCount;
    private int streakMultiplier = 1;

    private void Awake()
    {
        Application.targetFrameRate = 120;
        BuildMaterials();
        BuildWorld();
        BuildCamera();
        BuildAudio();
    }

    private void Update()
    {
        if (state == GameState.Playing)
        {
            songTime = Time.unscaledTime - pauseTime;
            UpdateNotes();
            ReadInput();
            if (songTime >= SongLength + TravelTime)
                FinishSong();
        }
        else if (state == GameState.Menu || state == GameState.Results)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                StartSong();
        }
        else if (state == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
        {
            ResumeSong();
        }

        if (state == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
            PauseSong();
    }

    private void BuildMaterials()
    {
        darkMaterial = MakeMaterial(new Color(0.025f, 0.035f, 0.085f), 0.05f);
        railMaterial = MakeMaterial(new Color(0.12f, 0.18f, 0.34f), 0.35f);
        hitMaterial = MakeMaterial(new Color(0.75f, 0.95f, 1f), 1.7f);
        laneMaterials = new Material[4];
        noteMaterials = new Material[4];
        for (int i = 0; i < 4; i++)
        {
            laneMaterials[i] = MakeMaterial(Color.Lerp(LaneColors[i], Color.black, 0.58f), 0.25f);
            noteMaterials[i] = MakeMaterial(LaneColors[i], 2.4f);
        }
    }

    private static Material MakeMaterial(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        return material;
    }

    private void BuildWorld()
    {
        RenderSettings.ambientLight = new Color(0.24f, 0.27f, 0.42f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.025f, 0.035f, 0.095f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.012f;
        AddCube("Stage", new Vector3(0, -0.62f, 12f), new Vector3(8.7f, 0.35f, 34f), darkMaterial);
        AddCube("Back wall", new Vector3(0, 3.8f, 29f), new Vector3(18f, 8f, 0.4f), darkMaterial);

        for (int lane = 0; lane < 4; lane++)
        {
            float x = LaneX(lane);
            AddCube("Lane " + (lane + 1), new Vector3(x, -0.38f, 12f), new Vector3(1.56f, 0.12f, 32f), laneMaterials[lane]);
            AddCube("Lane glow rail", new Vector3(x - 0.79f, -0.28f, 12f), new Vector3(0.045f, 0.07f, 32f), noteMaterials[lane]);
            GameObject pad = AddCube("Hit pad " + (lane + 1), new Vector3(x, -0.16f, HitZ), new Vector3(1.28f, 0.18f, 0.66f), noteMaterials[lane]);
            pad.transform.localScale = new Vector3(1.28f, 0.18f, 0.66f);
        }

        for (int z = -3; z < 29; z += 2)
            AddCube("Beat marker", new Vector3(0, -0.285f, z), new Vector3(6.7f, 0.025f, 0.035f), railMaterial);
        AddCube("Judgement line", new Vector3(0, -0.12f, HitZ), new Vector3(7.0f, 0.035f, 0.08f), hitMaterial);

        // Abstract light pillars give the stage a distinctive, generated arcade look.
        for (int i = 0; i < 12; i++)
        {
            float x = (i - 5.5f) * 1.35f;
            float height = 2.5f + (i % 4) * 0.7f;
            AddCube("Neon pillar", new Vector3(x, height * 0.5f - 0.1f, 27.5f), new Vector3(0.12f, height, 0.16f), noteMaterials[i % 4]);
        }
        AddCube("Stage header", new Vector3(0, 5.5f, 28.5f), new Vector3(12f, 0.12f, 0.22f), hitMaterial);
    }

    private GameObject AddCube(string objectName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = objectName;
        obj.transform.SetParent(transform, false);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        sceneObjects.Add(obj);
        return obj;
    }

    private void BuildCamera()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            GameObject cameraObject = new GameObject("Neon Riff Camera");
            gameCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }
        gameCamera.transform.position = new Vector3(0f, 7.3f, -8.7f);
        gameCamera.transform.rotation = Quaternion.Euler(16.5f, 0f, 0f);
        gameCamera.fieldOfView = 58f;
        gameCamera.backgroundColor = new Color(0.025f, 0.035f, 0.095f);
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
    }

    private void BuildAudio()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0.65f;
        musicSource.clip = CreateOriginalDemoTrack();
    }

    // A small original synthesized backing track, generated at runtime so the demo needs no external assets.
    private static AudioClip CreateOriginalDemoTrack()
    {
        const int sampleRate = 22050;
        int sampleCount = sampleRate * (int)SongLength;
        float[] samples = new float[sampleCount];
        const float beatSeconds = 0.5f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float beatPhase = t % beatSeconds;
            int beat = (int)(t / beatSeconds) % 4;
            float value = 0f;
            if (beatPhase < 0.18f && (beat == 0 || beat == 2))
            {
                float envelope = Mathf.Exp(-beatPhase * 23f);
                value += Mathf.Sin(2f * Mathf.PI * (72f - beatPhase * 180f) * beatPhase) * envelope * 0.42f;
            }
            if (beatPhase < 0.14f && (beat == 1 || beat == 3))
            {
                float envelope = Mathf.Exp(-beatPhase * 29f);
                float noise = Mathf.Sin(i * 12.9898f) * Mathf.Sin(i * 0.017f);
                value += noise * envelope * 0.20f;
            }
            float hatPhase = t % 0.25f;
            if (hatPhase < 0.035f)
                value += Mathf.Sin(i * 91.7f) * Mathf.Exp(-hatPhase * 90f) * 0.055f;
            int bassStep = ((int)(t / beatSeconds) % 8);
            float[] bassNotes = { 55f, 55f, 65.41f, 55f, 73.42f, 65.41f, 55f, 49f };
            float bassEnvelope = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * beatPhase / beatSeconds);
            value += Mathf.Sin(2f * Mathf.PI * bassNotes[bassStep] * t) * bassEnvelope * 0.09f;
            samples[i] = Mathf.Clamp(value, -0.9f, 0.9f);
        }
        AudioClip clip = AudioClip.Create("Neon Riff - Original Demo", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void StartSong()
    {
        ClearChart();
        score = combo = bestCombo = perfectCount = goodCount = missCount = 0;
        health = 100;
        streakMultiplier = 1;
        judgement = "GO!";
        lastJudgementTime = Time.unscaledTime;
        songTime = 0f;
        pauseTime = Time.unscaledTime;
        BuildChart();
        state = GameState.Playing;
        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.Play();
    }

    private void BuildChart()
    {
        float interval = difficulty == Difficulty.Chill ? 0.5f : difficulty == Difficulty.Voltage ? 0.5f : 0.375f;
        int total = Mathf.FloorToInt((SongLength - 1f) / interval);
        int previousLane = -1;
        for (int i = 0; i < total; i++)
        {
            float time = 1.25f + i * interval;
            if (time >= SongLength) break;
            int lane = UnityEngine.Random.Range(0, 4);
            if (lane == previousLane && i % 3 != 0) lane = (lane + 1 + UnityEngine.Random.Range(0, 3)) % 4;
            previousLane = lane;
            if (difficulty == Difficulty.Chill && i % 5 == 4) continue;
            if (difficulty == Difficulty.Voltage && i % 7 == 6) continue;
            CreateChartNote(lane, time);
            if (difficulty == Difficulty.Overdrive && i % 4 == 2)
                CreateChartNote((lane + 2) % 4, time + interval * 0.5f);
        }
    }

    private void CreateChartNote(int lane, float time)
    {
        GameObject note = GameObject.CreatePrimitive(PrimitiveType.Cube);
        note.name = "Riff note - lane " + (lane + 1);
        note.transform.SetParent(transform, false);
        note.transform.localScale = new Vector3(1.0f, 0.22f, 0.72f);
        note.GetComponent<Renderer>().sharedMaterial = noteMaterials[lane];
        Collider collider = note.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        chart.Add(new ChartNote { lane = lane, time = time, visual = note });
    }

    private void UpdateNotes()
    {
        for (int i = chart.Count - 1; i >= 0; i--)
        {
            ChartNote note = chart[i];
            if (note.resolved) continue;
            float remaining = note.time - songTime;
            note.visual.transform.position = new Vector3(LaneX(note.lane), -0.04f, HitZ + remaining * (28f / TravelTime));
            if (remaining < -MissWindow)
                ResolveMiss(note);
        }
    }

    private void ReadInput()
    {
        for (int lane = 0; lane < LaneKeys.Length; lane++)
        {
            if (Input.GetKeyDown(LaneKeys[lane])) HitLane(lane);
        }
    }

    private void HitLane(int lane)
    {
        ChartNote closest = null;
        float closestDelta = float.MaxValue;
        foreach (ChartNote note in chart)
        {
            if (note.resolved || note.lane != lane) continue;
            float delta = Mathf.Abs(note.time - songTime);
            if (delta < closestDelta) { closest = note; closestDelta = delta; }
        }
        if (closest == null || closestDelta > GoodWindow)
        {
            judgement = "MISS";
            lastJudgementTime = Time.unscaledTime;
            combo = 0;
            streakMultiplier = 1;
            health = Mathf.Max(0, health - 4);
            missCount++;
            if (health == 0) FinishSong();
            return;
        }

        closest.resolved = true;
        if (closest.visual != null) Destroy(closest.visual);
        if (closestDelta <= PerfectWindow)
        {
            judgement = "PERFECT";
            score += 1000 * streakMultiplier;
            perfectCount++;
            health = Mathf.Min(100, health + 1);
        }
        else
        {
            judgement = "GOOD";
            score += 600 * streakMultiplier;
            goodCount++;
        }
        combo++;
        bestCombo = Mathf.Max(bestCombo, combo);
        if (combo > 0 && combo % 10 == 0) streakMultiplier = Mathf.Min(4, streakMultiplier + 1);
        lastJudgementTime = Time.unscaledTime;
    }

    private void ResolveMiss(ChartNote note)
    {
        note.resolved = true;
        if (note.visual != null) Destroy(note.visual);
        combo = 0;
        streakMultiplier = 1;
        health = Mathf.Max(0, health - 7);
        missCount++;
        judgement = "MISSED NOTE";
        lastJudgementTime = Time.unscaledTime;
        if (health == 0) FinishSong();
    }

    private void PauseSong()
    {
        state = GameState.Paused;
        pauseTime = Time.unscaledTime - songTime;
        musicSource.Pause();
    }

    private void ResumeSong()
    {
        state = GameState.Playing;
        pauseTime = Time.unscaledTime - songTime;
        musicSource.UnPause();
    }

    private void FinishSong()
    {
        if (state != GameState.Playing) return;
        state = GameState.Results;
        musicSource.Stop();
        ClearChart();
    }

    private void ClearChart()
    {
        foreach (ChartNote note in chart)
            if (note.visual != null) Destroy(note.visual);
        chart.Clear();
    }

    private static float LaneX(int lane) { return (lane - 1.5f) * LaneSpacing; }

    private void OnGUI()
    {
        EnsureStyles();
        Matrix4x4 oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1920f * scale) * 0.5f, (Screen.height - 1080f * scale) * 0.5f, 0), Quaternion.identity, new Vector3(scale, scale, 1));

        if (state == GameState.Playing || state == GameState.Paused)
            DrawHud();
        if (state == GameState.Menu)
            DrawMenu();
        else if (state == GameState.Paused)
            DrawPause();
        else if (state == GameState.Results)
            DrawResults();
        GUI.matrix = oldMatrix;
    }

    private void DrawHud()
    {
        GUI.color = new Color(0.015f, 0.025f, 0.07f, 0.82f);
        GUI.Box(new Rect(35, 28, 1850, 118), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(70, 40, 440, 68), "SCORE  " + score.ToString("N0"), scoreStyle);
        GUI.Label(new Rect(600, 48, 350, 60), "COMBO  " + combo + "   x" + streakMultiplier, headingStyle);
        GUI.Label(new Rect(1090, 48, 350, 60), "BEST  " + bestCombo, bodyStyle);
        GUI.Label(new Rect(1570, 48, 250, 60), "HEALTH  " + health + "%", bodyStyle);
        GUI.color = new Color(0.2f, 0.9f, 0.86f, 0.9f);
        GUI.Box(new Rect(70, 126, 1770f * health / 100f, 7), GUIContent.none);
        GUI.color = Color.white;
        if (Time.unscaledTime - lastJudgementTime < 0.75f)
            GUI.Label(new Rect(700, 785, 520, 90), judgement, titleStyle);
        GUI.Label(new Rect(70, 975, 800, 42), "A     S     D     F     /     ESC TO PAUSE", smallStyle);
        GUI.Label(new Rect(1570, 975, 280, 42), FormatTime(songTime) + " / 0:32", smallStyle);
    }

    private void DrawMenu()
    {
        GUI.color = new Color(0.012f, 0.02f, 0.06f, 0.83f);
        GUI.Box(new Rect(475, 145, 970, 765), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(520, 205, 880, 120), "NEON RIFF", titleStyle);
        GUI.Label(new Rect(560, 330, 800, 56), "A rhythm rush through the afterglow.", headingStyle);
        GUI.Label(new Rect(590, 445, 740, 46), "CHOOSE YOUR INTENSITY", smallStyle);
        string[] levels = { "CHILL", "VOLTAGE", "OVERDRIVE" };
        for (int i = 0; i < levels.Length; i++)
        {
            GUI.color = (int)difficulty == i ? LaneColors[i] : Color.white;
            if (GUI.Button(new Rect(565 + i * 270, 510, 250, 72), levels[i], buttonStyle))
                difficulty = (Difficulty)i;
        }
        GUI.color = Color.white;
        if (GUI.Button(new Rect(685, 635, 550, 100), "START THE SHOW", buttonStyle)) StartSong();
        GUI.Label(new Rect(575, 765, 770, 42), "HIT NOTES WITH  A   S   D   F   WHEN THEY REACH THE LINE", smallStyle);
        GUI.Label(new Rect(640, 820, 650, 38), "Original procedural soundtrack  •  No external assets needed", smallStyle);
    }

    private void DrawPause()
    {
        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.88f);
        GUI.Box(new Rect(580, 300, 760, 420), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(690, 350, 540, 95), "PAUSED", titleStyle);
        if (GUI.Button(new Rect(720, 500, 480, 82), "RESUME  (ESC)", buttonStyle)) ResumeSong();
        if (GUI.Button(new Rect(720, 605, 480, 68), "BACK TO MENU", buttonStyle))
        {
            musicSource.Stop();
            ClearChart();
            state = GameState.Menu;
        }
    }

    private void DrawResults()
    {
        GUI.color = new Color(0.012f, 0.02f, 0.06f, 0.88f);
        GUI.Box(new Rect(475, 145, 970, 765), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(570, 205, 780, 110), "SHOW COMPLETE", titleStyle);
        GUI.Label(new Rect(670, 365, 600, 60), "FINAL SCORE   " + score.ToString("N0"), headingStyle);
        GUI.Label(new Rect(650, 450, 640, 48), "BEST COMBO   " + bestCombo + "     PERFECT   " + perfectCount, bodyStyle);
        GUI.Label(new Rect(700, 510, 540, 48), "GOOD   " + goodCount + "     MISSES   " + missCount, bodyStyle);
        GUI.Label(new Rect(690, 590, 560, 44), "RATING   " + GetRating(), headingStyle);
        if (GUI.Button(new Rect(685, 700, 550, 88), "PLAY AGAIN", buttonStyle)) StartSong();
        GUI.Label(new Rect(715, 820, 500, 38), "ENTER OR SPACE TO REPLAY", smallStyle);
    }

    private string GetRating()
    {
        int total = perfectCount + goodCount + missCount;
        float accuracy = total == 0 ? 0 : (perfectCount + goodCount * 0.65f) / total;
        if (accuracy >= 0.95f) return "S  •  ELECTRIC";
        if (accuracy >= 0.82f) return "A  •  BRIGHT";
        if (accuracy >= 0.65f) return "B  •  SOLID";
        return "C  •  KEEP RIFFING";
    }

    private static string FormatTime(float time)
    {
        int seconds = Mathf.Max(0, (int)time);
        return (seconds / 60) + ":" + (seconds % 60).ToString("00");
    }

    private void EnsureStyles()
    {
        if (stylesReady) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 84, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.35f, 1f, 0.95f) } };
        headingStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 36, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.8f, 0.88f, 1f) } };
        smallStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.57f, 0.69f, 0.82f) } };
        buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 27, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, hover = { textColor = new Color(0.35f, 1f, 0.95f) } };
        scoreStyle = new GUIStyle(headingStyle) { alignment = TextAnchor.MiddleLeft, fontSize = 40, normal = { textColor = new Color(0.35f, 1f, 0.95f) } };
        stylesReady = true;
    }

    private void OnDestroy()
    {
        if (musicSource != null && musicSource.clip != null) Destroy(musicSource.clip);
        foreach (Material material in laneMaterials ?? new Material[0]) if (material != null) Destroy(material);
        foreach (Material material in noteMaterials ?? new Material[0]) if (material != null) Destroy(material);
        if (darkMaterial != null) Destroy(darkMaterial);
        if (railMaterial != null) Destroy(railMaterial);
        if (hitMaterial != null) Destroy(hitMaterial);
    }
}
