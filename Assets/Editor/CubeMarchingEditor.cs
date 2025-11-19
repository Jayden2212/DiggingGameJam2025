#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Provides a menu item to create a marching-terrain GameObject and a simple inspector button
public class CubeMarchingEditor
{
    [MenuItem("GameObject/3D Object/Marching Terrain", false, 10)]
    static void CreateMarchingTerrain(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("MarchingTerrain");
        // place in the scene
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mc = go.AddComponent<MeshCollider>();
        // add the runtime CubeMarching component
        var cm = go.AddComponent(typeof(CubeMarching));

        // Try to assign a default material if available
        var defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
        if (defaultMat != null)
            mr.sharedMaterial = defaultMat;

        // Select the new object
        Selection.activeGameObject = go;
    }
}

// Custom inspector for CubeMarching
[CustomEditor(typeof(CubeMarching))]
public class CubeMarchingCustomInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        // If the target has CubeMarching, show a button. Avoid compile-time dependency issues by reflection.
        var t = target as MonoBehaviour;
        if (t == null) return;

        var type = t.GetType();
        var method = type.GetMethod("GenerateInEditor");
        if (method != null)
        {
            if (GUILayout.Button("Generate Mesh (Editor)"))
            {
                method.Invoke(t, null);
            }
        }
    }
}
#endif
