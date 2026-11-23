using Unity.Profiling;
using UnityEngine;

public sealed class RadarMap : MonoBehaviour
{
    const int MapSize = 512;
    static readonly ProfilerMarker UpdateMap = new ProfilerMarker("Radar.UpdateMap");
    static readonly Color32 Ocean = new Color32(12, 32, 39, 255);
    static readonly Color32 Shore = new Color32(115, 190, 154, 255);
    static readonly Color32 Land = new Color32(39, 94, 81, 255);

    public bool rebuildEveryFrame;
    Texture2D map;
    Texture2D displayedMap;
    float nextReading;
    string cost = "";
    readonly System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

    void Awake()
    {
        map = new Texture2D(MapSize, MapSize, TextureFormat.RGBA32, false);
        map.filterMode = FilterMode.Point;
        BakeTerrain();
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color32(9, 22, 28, 255);
    }

    void BakeTerrain()
    {
        var pixels = new Color32[MapSize * MapSize];
        for (int y = 0; y < MapSize; y++)
        {
            for (int x = 0; x < MapSize; x++)
            {
                float u = (x - MapSize * .5f) / (MapSize * .5f);
                float v = (y - MapSize * .5f) / (MapSize * .5f);
                float h = Mathf.PerlinNoise(x * .0095f, y * .0095f) * .62f
                        + Mathf.PerlinNoise(x * .0215f + 11, y * .0215f) * .25f
                        + Mathf.PerlinNoise(x * .049f, y * .049f + 23) * .13f
                        - .16f * (u * u + v * v);
                bool contour = h >= .46f && Mathf.Repeat(h - .46f, .055f) < .008f;
                pixels[y * MapSize + x] = contour ? Shore : h >= .46f ? Land : Ocean;
            }
        }
        map.SetPixels32(pixels);
        map.Apply(false);
    }

    void Update()
    {
        timer.Restart();
        using (UpdateMap.Auto())
        {
            if (rebuildEveryFrame)
                BakeTerrain();
            displayedMap = map;
        }
        timer.Stop();
        if (Time.unscaledTime >= nextReading)
        {
            double ms = timer.Elapsed.TotalMilliseconds;
            cost = "Map update: " + (ms < .01 ? "< 0.01" : ms.ToString("0.00")) + " ms";
            nextReading = Time.unscaledTime + .25f;
        }
    }

    void OnGUI()
    {
        float scale = Mathf.Min(Screen.width / 560f, Screen.height / 400f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 560 * scale) / 2,
            (Screen.height - 400 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
        var chart = new Rect(16, 16, 528, 330);
        if (displayedMap)
            GUI.DrawTextureWithTexCoords(chart, displayedMap, new Rect(0, .1875f, 1, .625f));
        GUI.color = new Color(.51f, .85f, .69f, .18f);
        for (int x = 0; x <= 528; x += 66)
            GUI.DrawTexture(new Rect(chart.x + x, chart.y, 1, chart.height), Texture2D.whiteTexture);
        for (int y = 0; y <= 330; y += 66)
            GUI.DrawTexture(new Rect(chart.x, chart.y + y, chart.width, 1), Texture2D.whiteTexture);
        GUI.color = new Color(.68f, 1f, .64f, .88f);
        float scan = Mathf.Repeat(Time.time * 85, chart.width);
        GUI.DrawTexture(new Rect(chart.x + scan, chart.y, 2, chart.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        rebuildEveryFrame = GUI.Toggle(new Rect(16, 358, 240, 28),
            rebuildEveryFrame, "Rebuild terrain every frame");
        GUI.Label(new Rect(366, 357, 178, 28), cost);
    }

    void OnDestroy() => Destroy(map);
}
