using System.IO;
using UnityEditor;
using UnityEngine;

public static class ToiletMechBossResourcePreparer
{
    private const string TargetFolder = "Assets/Resources/Models/Boss";
    private const string TargetPrefabPath = TargetFolder + "/ToiletMech_Boss.prefab";
    private static readonly string[] PreferredSourcePaths =
    {
        "Assets/Sci-Fi ToiletMech/Prefab/Sci-Fi ToiletMech Skin 1.prefab",
        "Assets/Sci-Fi ToiletMech/Prefab/Sci-Fi ToiletMech Skin 2.prefab",
        "Assets/Sci-Fi ToiletMech/Prefab/Sci-Fi ToiletMech Skin 3.prefab",
        "Assets/Sci-Fi ToiletMech/Prefab/Sci-Fi ToiletMech Skin 4.prefab"
    };

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
        for (int i = 0; i < PreferredSourcePaths.Length; i++)
        {
            if (File.Exists(PreferredSourcePaths[i]))
            {
                return PreferredSourcePaths[i];
            }
        }

        string[] guids = AssetDatabase.FindAssets("ToiletMech t:GameObject");

        if (guids.Length == 0)
        {
            guids = AssetDatabase.FindAssets("Toilet Mech t:GameObject");
        }

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string lowerPath = path.ToLowerInvariant();

            if (lowerPath.Contains("toiletmech") && lowerPath.Contains("/prefab/") && !lowerPath.Contains("props"))
            {
                return path;
            }
        }

        return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : string.Empty;
    }
}
