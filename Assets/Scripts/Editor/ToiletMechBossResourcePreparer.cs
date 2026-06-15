using System.IO;
using UnityEditor;
using UnityEngine;

public static class ToiletMechBossResourcePreparer
{
    private const string TargetFolder = "Assets/Resources/Models/Boss";
    private const string TargetPrefabPath = TargetFolder + "/ToiletMech_Boss.prefab";

    [MenuItem("Stargrave/Prepare ToiletMech Boss Resource")]
    public static void Prepare()
    {
        string sourcePath = FindToiletMechAssetPath();

        if (string.IsNullOrEmpty(sourcePath))
        {
            EditorUtility.DisplayDialog(
                "ToiletMech not found",
                "Import the Unity Asset Store package first, then run this menu again.",
                "OK"
            );
            return;
        }

        Directory.CreateDirectory(TargetFolder);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);

        if (source == null)
        {
            EditorUtility.DisplayDialog("Invalid asset", "Found a ToiletMech asset, but it is not a GameObject.", "OK");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = "ToiletMech_Boss";
        PrefabUtility.SaveAsPrefabAsset(instance, TargetPrefabPath);
        Object.DestroyImmediate(instance);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("ToiletMech ready", "Created " + TargetPrefabPath, "OK");
    }

    private static string FindToiletMechAssetPath()
    {
        string[] guids = AssetDatabase.FindAssets("ToiletMech t:GameObject");

        if (guids.Length == 0)
        {
            guids = AssetDatabase.FindAssets("Toilet Mech t:GameObject");
        }

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string lowerPath = path.ToLowerInvariant();

            if (lowerPath.Contains("toilet") && lowerPath.Contains("mech"))
            {
                return path;
            }
        }

        return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : string.Empty;
    }
}
