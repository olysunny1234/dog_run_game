using UnityEngine;

/// <summary>
/// Add this component to a dog root GameObject to build the full
/// quadruped appearance at runtime. The root should sit at y=0
/// on the ground; all parts are created as children.
/// </summary>
public class DogAppearance : MonoBehaviour
{
    [SerializeField] private Color furColor = new Color(0.75f, 0.52f, 0.30f);

    private void Start()
    {
        BuildDogGeometry(furColor);
    }

    private void BuildDogGeometry(Color fur)
    {
        Material furMat  = MakeMat(fur, 0.10f);
        Material darkFur = MakeMat(fur * 0.72f, 0.10f);
        Material noseMat = MakeMat(new Color(0.04f, 0.04f, 0.04f), 0.88f);
        noseMat.SetFloat("_Metallic", 0.12f);
        Material eyeWhite = MakeMat(Color.white, 0.70f);
        Material eyeIris  = MakeMat(new Color(0.22f, 0.13f, 0.04f), 0.55f);
        Material eyePupil = MakeMat(Color.black, 0.80f);

        // Body
        Part("Body", PrimitiveType.Sphere,
            new Vector3(0f, 0.88f, 0f), new Vector3(0.65f, 0.50f, 1.22f), furMat);

        // Legs + paws
        float legX = 0.22f, frontZ = 0.38f, backZ = -0.36f, legScaleY = 0.32f;
        foreach (var (nm, x, z) in new[]{
            ("FrontLeftLeg",  -legX,  frontZ),
            ("FrontRightLeg",  legX,  frontZ),
            ("BackLeftLeg",   -legX,  backZ),
            ("BackRightLeg",   legX,  backZ)})
        {
            Part(nm, PrimitiveType.Cylinder,
                new Vector3(x, legScaleY, z), new Vector3(0.13f, legScaleY, 0.13f), furMat);
            Part(nm + "Paw", PrimitiveType.Sphere,
                new Vector3(x, 0.055f, z + (z > 0 ? 0.06f : -0.06f)),
                new Vector3(0.18f, 0.09f, 0.22f), darkFur);
        }

        // Head & snout
        Part("Head",  PrimitiveType.Sphere,
            new Vector3(0f, 1.12f, 0.68f), new Vector3(0.50f, 0.47f, 0.50f), furMat);
        Part("Snout", PrimitiveType.Sphere,
            new Vector3(0f, 0.98f, 1.00f), new Vector3(0.27f, 0.19f, 0.36f), furMat);

        // Nose
        var nose = Part("Nose", PrimitiveType.Sphere,
            new Vector3(0f, 0.97f, 1.18f), new Vector3(0.11f, 0.09f, 0.07f), noseMat);

        // Floppy ears
        foreach (var (nm, x, zRot) in new[]{
            ("LeftEar",  -0.30f,  28f),
            ("RightEar",  0.30f, -28f)})
        {
            var ear = Part(nm, PrimitiveType.Sphere,
                new Vector3(x, 1.26f, 0.66f), new Vector3(0.14f, 0.38f, 0.09f), darkFur);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        }

        // Eyes
        foreach (var (side, ex) in new[]{ ("Left", -0.185f), ("Right", 0.185f) })
        {
            Part(side + "EyeWhite", PrimitiveType.Sphere,
                new Vector3(ex, 1.15f, 0.91f),  new Vector3(0.10f, 0.10f, 0.05f), eyeWhite);
            Part(side + "EyeIris",  PrimitiveType.Sphere,
                new Vector3(ex, 1.15f, 0.935f), new Vector3(0.07f, 0.07f, 0.04f), eyeIris);
            Part(side + "EyePupil", PrimitiveType.Sphere,
                new Vector3(ex, 1.15f, 0.958f), new Vector3(0.04f, 0.05f, 0.03f), eyePupil);
        }

        // Tail
        var tail = Part("Tail", PrimitiveType.Cylinder,
            new Vector3(0f, 1.02f, -0.64f), new Vector3(0.07f, 0.22f, 0.07f), darkFur);
        tail.transform.localRotation = Quaternion.Euler(-52f, 0f, 0f);
    }

    private GameObject Part(string nm, PrimitiveType prim,
        Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(prim);
        go.name = nm;
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return go;
    }

    private static Material MakeMat(Color color, float smoothness)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        return mat;
    }
}
