using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode normalizeMode;
    public int seed;
    public float noiseScale;
    public Vector2 offset;

    [Range(0, 6)]
    public float lacunarity;

    [Range(0, 1)]
    public float persistance;

    [Range(1, 25)]
    public int numOctaves;
    

    protected override void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;
        if (numOctaves < 0) numOctaves = 0;

        base.OnValidate();
    }
}
