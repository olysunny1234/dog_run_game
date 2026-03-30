using UnityEngine;

public class LawnSceneSetup : MonoBehaviour
{
    // Shared grass materials — reused across blades so Unity can batch them
    private Material[] _grassMats;

    private void Awake()
    {
        CreateGrassMaterials();
        CreateLawnScene();
    }

    private void CreateGrassMaterials()
    {
        _grassMats = new Material[]
        {
            MakeMat(new Color(0.13f, 0.50f, 0.13f), 0.08f), // mid green
            MakeMat(new Color(0.10f, 0.42f, 0.10f), 0.06f), // dark green
            MakeMat(new Color(0.20f, 0.62f, 0.14f), 0.10f), // bright green
            MakeMat(new Color(0.22f, 0.55f, 0.10f), 0.07f), // yellow-green
        };
        foreach (var m in _grassMats)
            m.enableInstancing = true;
    }

    private void CreateLawnScene()
    {
        CreateLawnGround();
        ScatterGrassBlades();
        CreateDogs();
        CreateEnvironment();
        SetupCameraAndLighting();
        Debug.Log("Lawn scene setup complete!");
    }

    // ─── Lawn ────────────────────────────────────────────────────────────────

    private void CreateLawnGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "LawnGround";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(5f, 1f, 5f);

        // Matte dark-green base — low smoothness so it doesn't look plastic
        Material mat = MakeMat(new Color(0.14f, 0.44f, 0.14f), 0.04f);
        ground.GetComponent<Renderer>().material = mat;
    }

    private void ScatterGrassBlades()
    {
        // 130 clumps × ~5 blades = ~650 objects; cubes have 24 verts so Unity
        // dynamic-batches them when they share the same material.
        int clumps = 130;
        for (int i = 0; i < clumps; i++)
        {
            Vector3 center = new Vector3(
                Random.Range(-10f, 10f),
                0f,
                Random.Range(-10f, 10f));

            int bladesInClump = Random.Range(3, 7);
            for (int j = 0; j < bladesInClump; j++)
            {
                Vector3 pos = center + new Vector3(
                    Random.Range(-0.35f, 0.35f),
                    0f,
                    Random.Range(-0.35f, 0.35f));
                SpawnBlade(pos);
            }
        }
    }

    private void SpawnBlade(Vector3 basePos)
    {
        float h = Random.Range(0.07f, 0.22f);
        float w = Random.Range(0.025f, 0.055f);

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "GrassBlade";
        blade.transform.position = basePos + new Vector3(0f, h * 0.5f, 0f);
        blade.transform.localScale = new Vector3(w, h, w * 0.45f);
        blade.transform.rotation = Quaternion.Euler(
            Random.Range(-18f, 18f),
            Random.Range(0f, 360f),
            Random.Range(-18f, 18f));

        // Remove collider — grass blades don't need physics
        Destroy(blade.GetComponent<BoxCollider>());

        blade.GetComponent<Renderer>().material = _grassMats[Random.Range(0, _grassMats.Length)];
    }

    // ─── Dog ─────────────────────────────────────────────────────────────────

    private void CreateDogs()
    {
        // Dogs face +Z; root sits at y=0 on the ground
        CreateDog("Dog1", new Vector3(-3f, 0f, 0f), new Color(0.75f, 0.52f, 0.30f)); // golden brown
        CreateDog("Dog2", new Vector3( 3f, 0f, 0f), new Color(0.88f, 0.72f, 0.18f)); // yellow lab
    }

    private GameObject CreateDog(string name, Vector3 position, Color furColor)
    {
        // Empty root at ground level — all parts are children
        GameObject root = new GameObject(name);
        root.transform.position = position;

        root.AddComponent<AudioSource>();
        root.AddComponent<Dog>();

        BuildDogGeometry(root, furColor);

        return root;
    }

    /// <summary>
    /// Builds a proper quadruped: horizontal barrel body, separate spherical
    /// head with snout, four legs with paws, floppy ears, layered eyes, shiny
    /// nose, and an upswept tail.
    /// All positions are local to the dog root (which sits at y=0 on grass).
    /// </summary>
    private void BuildDogGeometry(GameObject dog, Color fur)
    {
        Material furMat  = MakeMat(fur, 0.10f);               // matte fur
        Material darkFur = MakeMat(fur * 0.72f, 0.10f);       // darker underside / ears
        Material noseMat = MakeMat(new Color(0.04f, 0.04f, 0.04f), 0.88f); // wet nose
        noseMat.SetFloat("_Metallic", 0.12f);
        Material eyeWhite = MakeMat(Color.white, 0.70f);
        Material eyeIris  = MakeMat(new Color(0.22f, 0.13f, 0.04f), 0.55f);
        Material eyePupil = MakeMat(Color.black, 0.80f);

        // ── Body (barrel-shaped sphere elongated in Z) ────────────────────
        // Center y=0.88; visual half-height in Y ≈ 0.5×0.5=0.25 → bottom y≈0.63
        Part("Body", dog, PrimitiveType.Sphere,
            localPos: new Vector3(0f, 0.88f, 0f),
            localScale: new Vector3(0.65f, 0.50f, 1.22f),
            mat: furMat);

        // ── Four legs ────────────────────────────────────────────────────
        // Unity Cylinder height = localScale.y × 2 (extends ±scale.y from centre)
        // scale.y = 0.32 → height = 0.64; centre at y=0.32 → top=0.64 meets body bottom
        float legY = 0.32f;
        float legScaleY = 0.32f;   // actual height = 0.64
        float legX = 0.22f;
        float frontZ = 0.38f;
        float backZ  = -0.36f;

        foreach (var (nm, x, z) in new[]{
            ("FrontLeftLeg",  -legX,  frontZ),
            ("FrontRightLeg",  legX,  frontZ),
            ("BackLeftLeg",   -legX,  backZ),
            ("BackRightLeg",   legX,  backZ)})
        {
            Part(nm, dog, PrimitiveType.Cylinder,
                localPos: new Vector3(x, legY, z),
                localScale: new Vector3(0.13f, legScaleY, 0.13f),
                mat: furMat);

            // Paw — slightly wider than leg, pushed forward a little
            Part(nm + "Paw", dog, PrimitiveType.Sphere,
                localPos: new Vector3(x, 0.055f, z + (z > 0 ? 0.06f : -0.06f)),
                localScale: new Vector3(0.18f, 0.09f, 0.22f),
                mat: darkFur);
        }

        // ── Head ─────────────────────────────────────────────────────────
        Part("Head", dog, PrimitiveType.Sphere,
            localPos: new Vector3(0f, 1.12f, 0.68f),
            localScale: new Vector3(0.50f, 0.47f, 0.50f),
            mat: furMat);

        // ── Snout (elongated forward from head) ───────────────────────────
        Part("Snout", dog, PrimitiveType.Sphere,
            localPos: new Vector3(0f, 0.98f, 1.00f),
            localScale: new Vector3(0.27f, 0.19f, 0.36f),
            mat: furMat);

        // ── Nose ──────────────────────────────────────────────────────────
        Part("Nose", dog, PrimitiveType.Sphere,
            localPos: new Vector3(0f, 0.97f, 1.18f),
            localScale: new Vector3(0.11f, 0.09f, 0.07f),
            mat: noseMat);

        // ── Floppy ears (hang down from the sides of the head) ────────────
        foreach (var (nm, x, zRot) in new[]{
            ("LeftEar",  -0.30f,  28f),
            ("RightEar",  0.30f, -28f)})
        {
            var ear = Part(nm, dog, PrimitiveType.Sphere,
                localPos: new Vector3(x, 1.26f, 0.66f),
                localScale: new Vector3(0.14f, 0.38f, 0.09f),
                mat: darkFur);
            ear.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);
        }

        // ── Eyes (white + iris + pupil) ───────────────────────────────────
        foreach (var (side, ex) in new[]{ ("Left", -0.185f), ("Right", 0.185f) })
        {
            Part(side + "EyeWhite", dog, PrimitiveType.Sphere,
                localPos: new Vector3(ex, 1.15f, 0.91f),
                localScale: new Vector3(0.10f, 0.10f, 0.05f),
                mat: eyeWhite);

            Part(side + "EyeIris", dog, PrimitiveType.Sphere,
                localPos: new Vector3(ex, 1.15f, 0.935f),
                localScale: new Vector3(0.07f, 0.07f, 0.04f),
                mat: eyeIris);

            Part(side + "EyePupil", dog, PrimitiveType.Sphere,
                localPos: new Vector3(ex, 1.15f, 0.958f),
                localScale: new Vector3(0.04f, 0.05f, 0.03f),
                mat: eyePupil);
        }

        // ── Tail (curves up behind the body) ─────────────────────────────
        var tailObj = Part("Tail", dog, PrimitiveType.Cylinder,
            localPos: new Vector3(0f, 1.02f, -0.64f),
            localScale: new Vector3(0.07f, 0.22f, 0.07f),
            mat: darkFur);
        tailObj.transform.localRotation = Quaternion.Euler(-52f, 0f, 0f);
    }

    /// <summary>Helper: create a primitive as a child of parent.</summary>
    private GameObject Part(string nm, GameObject parent, PrimitiveType prim,
        Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(prim);
        go.name = nm;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        go.GetComponent<Renderer>().material = mat;

        // Colliders on cosmetic sub-parts cause unnecessary physics overhead
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        return go;
    }

    // ─── Environment ─────────────────────────────────────────────────────────

    private void CreateEnvironment()
    {
        CreateTree(new Vector3(-8f, 0f, -8f));
        CreateTree(new Vector3( 8f, 0f, -8f));
        CreateTree(new Vector3(-8f, 0f,  8f));
        CreateTree(new Vector3( 8f, 0f,  8f));

        for (int i = 0; i < 8; i++)
            CreateBush(new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f)));

        CreateFence();
    }

    private void CreateTree(Vector3 position)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "TreeTrunk";
        trunk.transform.position = position + new Vector3(0f, 2f, 0f);
        trunk.transform.localScale = new Vector3(0.3f, 2f, 0.3f);
        trunk.GetComponent<Renderer>().material = MakeMat(new Color(0.36f, 0.20f, 0.09f), 0.15f);

        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.name = "TreeFoliage";
        foliage.transform.position = position + new Vector3(0f, 5.0f, 0f);
        foliage.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
        foliage.GetComponent<Renderer>().material = MakeMat(new Color(0.10f, 0.38f, 0.10f), 0.05f);
    }

    private void CreateBush(Vector3 position)
    {
        GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bush.name = "Bush";
        bush.transform.position = position + new Vector3(0f, 0.5f, 0f);
        float s = Random.Range(0.8f, 1.4f);
        bush.transform.localScale = new Vector3(s, s * 0.75f, s);
        bush.GetComponent<Renderer>().material = MakeMat(new Color(0.12f, 0.46f, 0.12f), 0.06f);
    }

    private void CreateFence()
    {
        const float half = 8f;
        Color wood = new Color(0.58f, 0.38f, 0.18f);
        Material woodMat = MakeMat(wood, 0.18f);

        for (float t = -half; t <= half; t += 2f)
        {
            SpawnPost(new Vector3( t,    0.75f, -half), woodMat);
            SpawnPost(new Vector3( t,    0.75f,  half), woodMat);
            SpawnPost(new Vector3(-half, 0.75f,  t   ), woodMat);
            SpawnPost(new Vector3( half, 0.75f,  t   ), woodMat);
        }
    }

    private void SpawnPost(Vector3 pos, Material mat)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "FencePost";
        post.transform.position = pos;
        post.transform.localScale = new Vector3(0.1f, 1.5f, 0.1f);
        post.GetComponent<Renderer>().material = mat;
    }

    // ─── Camera & Lighting ────────────────────────────────────────────────────

    private void SetupCameraAndLighting()
    {
        GameObject cam = GameObject.Find("MainCamera") ?? new GameObject("MainCamera");
        if (cam.GetComponent<Camera>() == null)   cam.AddComponent<Camera>();
        if (cam.GetComponent<AudioListener>() == null) cam.AddComponent<AudioListener>();
        cam.transform.position = new Vector3(0f, 4f, -7f);
        cam.transform.rotation = Quaternion.Euler(22f, 0f, 0f);

        GameObject sun = GameObject.Find("DirectionalLight") ?? new GameObject("DirectionalLight");
        Light light = sun.GetComponent<Light>() ?? sun.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.color     = new Color(1.0f, 0.96f, 0.82f); // warm sunlight
        light.intensity = 1.15f;
        sun.transform.rotation = Quaternion.Euler(48f, -42f, 0f);

        // Slight sky-blue ambient
        RenderSettings.ambientLight = new Color(0.38f, 0.46f, 0.58f);
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    private static Material MakeMat(Color color, float smoothness)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color      = color;
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        return mat;
    }
}
