using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    public void ApplyToMaterial(Material mat)
    {

    }

    public void UpdateMeshHeights(Material mat, float minHeight, float maxHeight)
    {
        mat.SetFloat("minHeight", minHeight);
        mat.SetFloat("maxHeight", maxHeight);
    }
}
