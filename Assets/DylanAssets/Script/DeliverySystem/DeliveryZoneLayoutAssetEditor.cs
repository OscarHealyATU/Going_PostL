using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeliveryZoneLayoutAsset))]
public class DeliveryZoneLayoutAssetEditor : Editor
{
    private gridify sourceGrid;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Copy From Gridify", EditorStyles.boldLabel);

        sourceGrid = (gridify)EditorGUILayout.ObjectField(
            "Source Grid",
            sourceGrid,
            typeof(gridify),
            true
        );

        GUI.enabled = sourceGrid != null;

        if (GUILayout.Button("Copy Values From Gridify"))
        {
            DeliveryZoneLayoutAsset asset = (DeliveryZoneLayoutAsset)target;
            Undo.RecordObject(asset, "Copy Delivery Layout From Gridify");
            asset.CopyFromGridify(sourceGrid);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        GUI.enabled = true;
    }
}