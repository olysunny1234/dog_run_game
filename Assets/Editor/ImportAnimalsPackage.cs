using UnityEngine;
using UnityEditor;

public class ImportAnimalsPackage
{
    [MenuItem("Tools/Import Animals Package")]
    public static void Import()
    {
        string path = @"C:\Users\xma\AppData\Roaming\Unity\Asset Store-5.x\ithappy\3D ModelsCharactersAnimals\Animals FREE - Animated Low Poly 3D Models.unitypackage";
        AssetDatabase.ImportPackage(path, false);
        Debug.Log("Animals package imported successfully!");
    }

    [MenuItem("Tools/Fix Animals Built-In Materials")]
    public static void FixMaterials()
    {
        string builtInPkg = "Assets/ithappy/Animals_FREE/Render_Pipeline_Convert/Unity_6_Built-In_source.unitypackage";
        string fullPath = System.IO.Path.GetFullPath(builtInPkg);
        AssetDatabase.ImportPackage(fullPath, false);
        Debug.Log("Built-In render pipeline materials imported!");
    }
}
