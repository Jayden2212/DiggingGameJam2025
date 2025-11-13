using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]

public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator terrainGenerator = (TerrainGenerator)target;

        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            terrainGenerator.GenerateTerrain();
        }

        if (GUILayout.Button("Generate"))
        {
            terrainGenerator.GenerateTerrain();
        }
    }
}
