using UnityEditor;
using UnityEngine;

public static class ImportGermanShepherd
{
    [MenuItem("Tools/Import German Shepherd")]
    public static void Import()
    {
        string path = @"C:/Users/xma/AppData/Roaming/Unity/Asset Store-5.x/RetroStyle Games/3D ModelsCharactersAnimals/German Shepherd 3D Model.unitypackage";
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.ImportPackage(path, false);
            Debug.Log("German Shepherd package imported successfully!");
        }
        else
        {
            Debug.LogError("Package not found at: " + path);
        }
    }
}