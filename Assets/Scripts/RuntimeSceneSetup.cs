using UnityEngine;

public class RuntimeSceneSetup : MonoBehaviour
{
    private Material[] _grassMats;

    private void Awake()
    {
        _grassMats = new Material[]
        {
            MakeMat(new Color(0.13f, 0.50f, 0.13f), 0.08f),
            MakeMat(new Color(0.10f, 0.42f, 0.10f), 0.06f),
            MakeMat(new Color(0.20f, 0.62f, 0.14f), 0.10f),
        };
        foreach (var m in _grassMats) m.enableInstancing = true;

        CreateLawnScene();
        Debug.Log("Runtime lawn scene created!");
    }

    private void CreateLawnScene()
    {
        // Ground
        GameObject lawn = GameObject.CreatePrimitive(PrimitiveType.Plane);
        lawn.name = "Lawn";
        lawn.transform.localScale = new Vector3(10f, 1f, 10f);
        lawn.GetComponent<Renderer>().material = MakeMat(new Color(0.14f, 0.44f, 0.14f), 0.04f);

        // Grass blades
        for (int i = 0; i < 100; i++)
        {
            Vector3 center = new Vector3(Random.Range(-14f, 14f), 0f, Random.Range(-14f, 14f));
            for (int j = 0; j < Random.Range(3, 6); j++)
                SpawnBlade(center + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f)));
        }

        // Dogs at ground level; geometry positions the body above
        CreateDog("DogA", new Vector3(-3f, 0f, 0f), new Color(0.75f, 0.52f, 0.30f));
        CreateDog("DogB", new Vector3( 3f, 0f, 0f), new Color(0.88f, 0.72f, 0.18f));
    }

    private void SpawnBlade(Vector3 basePos)
    {
        float h = Random.Range(0.07f, 0.22f), w = Random.Range(0.025f, 0.055f);
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        b.name = "GrassBlade";
        b.transform.position = basePos + new Vector3(0f, h * 0.5f, 0f);
        b.transform.localScale = new Vector3(w, h, w * 0.45f);
        b.transform.rotation = Quaternion.Euler(
            Random.Range(-18f, 18f), Random.Range(0f, 360f), Random.Range(-18f, 18f));
        Destroy(b.GetComponent<BoxCollider>());
        b.GetComponent<Renderer>().material = _grassMats[Random.Range(0, _grassMats.Length)];
    }

    private void CreateDog(string name, Vector3 position, Color fur)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;
        root.AddComponent<AudioSource>();
        root.AddComponent<Dog>();

        Material furMat  = MakeMat(fur, 0.10f);
        Material darkFur = MakeMat(fur * 0.72f, 0.10f);
        Material noseMat = MakeMat(new Color(0.04f, 0.04f, 0.04f), 0.88f);
        noseMat.SetFloat("_Metallic", 0.12f);

        Part("Body",  root, PrimitiveType.Sphere,
            new Vector3(0f, 0.88f, 0f), new Vector3(0.65f, 0.50f, 1.22f), furMat);

        float lx = 0.22f, fz = 0.38f, bz = -0.36f, ly = 0.32f;
        foreach (var (nm, x, z) in new[]{
            ("FrontLeftLeg",  -lx, fz), ("FrontRightLeg", lx, fz),
            ("BackLeftLeg",   -lx, bz), ("BackRightLeg",  lx, bz)})
        {
            Part(nm,         root, PrimitiveType.Cylinder,
                new Vector3(x, ly, z), new Vector3(0.13f, ly, 0.13f), furMat);
            Part(nm + "Paw", root, PrimitiveType.Sphere,
                new Vector3(x, 0.055f, z + (z > 0 ? 0.06f : -0.06f)),
                new Vector3(0.18f, 0.09f, 0.22f), darkFur);
        }

        Part("Head",  root, PrimitiveType.Sphere,
            new Vector3(0f, 1.12f, 0.68f), new Vector3(0.50f, 0.47f, 0.50f), furMat);
        Part("Snout", root, PrimitiveType.Sphere,
            new Vector3(0f, 0.98f, 1.00f), new Vector3(0.27f, 0.19f, 0.36f), furMat);
        Part("Nose",  root, PrimitiveType.Sphere,
            new Vector3(0f, 0.97f, 1.18f), new Vector3(0.11f, 0.09f, 0.07f), noseMat);

        foreach (var (side, ex, zr) in new[]{
            ("Left", -0.30f, 28f), ("Right", 0.30f, -28f)})
        {
            var ear = Part(side + "Ear", root, PrimitiveType.Sphere,
                new Vector3(ex, 1.26f, 0.66f), new Vector3(0.14f, 0.38f, 0.09f), darkFur);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, zr);
        }

        foreach (var (side, ex) in new[]{ ("Left", -0.185f), ("Right", 0.185f) })
        {
            Part(side + "EyeWhite", root, PrimitiveType.Sphere,
                new Vector3(ex, 1.15f, 0.91f),  new Vector3(0.10f, 0.10f, 0.05f),
                MakeMat(Color.white, 0.70f));
            Part(side + "EyeIris", root, PrimitiveType.Sphere,
                new Vector3(ex, 1.15f, 0.935f), new Vector3(0.07f, 0.07f, 0.04f),
                MakeMat(new Color(0.22f, 0.13f, 0.04f), 0.55f));
            Part(side + "EyePupil", root, PrimitiveType.Sphere,
                new Vector3(ex, 1.15f, 0.958f), new Vector3(0.04f, 0.05f, 0.03f),
                MakeMat(Color.black, 0.80f));
        }

        var tail = Part("Tail", root, PrimitiveType.Cylinder,
            new Vector3(0f, 1.02f, -0.64f), new Vector3(0.07f, 0.22f, 0.07f), darkFur);
        tail.transform.localRotation = Quaternion.Euler(-52f, 0f, 0f);
    }

    private GameObject Part(string nm, GameObject parent, PrimitiveType prim,
        Vector3 lPos, Vector3 lScale, Material mat)
    {
        var go = GameObject.CreatePrimitive(prim);
        go.name = nm;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.localPosition = lPos;
        go.transform.localScale    = lScale;
        go.GetComponent<Renderer>().material = mat;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return go;
    }

    private static Material MakeMat(Color c, float smoothness)
    {
        var m = new Material(Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Glossiness", smoothness);
        m.SetFloat("_Metallic", 0f);
        return m;
    }
}
