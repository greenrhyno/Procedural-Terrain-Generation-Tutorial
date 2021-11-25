using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public float uniformScale = 2.5f;

    public bool useFlatShading;
    public bool useFalloffMap;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

}
