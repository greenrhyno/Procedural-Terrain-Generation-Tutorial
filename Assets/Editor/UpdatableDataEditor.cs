using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpdatableData), true)]
[CanEditMultipleObjects]
class UpdatableDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        UpdatableData data = (UpdatableData)target;
        if (GUILayout.Button("Update"))
        {
            data.NotifyOfUpdatedValues();
        }
    }
}
