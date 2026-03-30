using UnityEngine;

/// <summary>
/// Builds the entire Dog Game scene at runtime.
/// Attach to a GameObject in the scene and press Play.
/// Controls: WASD / Arrow Keys = move   SPACE = bark
/// </summary>
public class DogGameSetup : MonoBehaviour
{
    [Header("Dog Prefab (assign Dog_001 from ithappy package)")]
    public GameObject dogPrefab;

    private Material[] _grassMats;
    private AudioClip[] _barkClips;

    // Kenney nature models loaded from Assets/Models/
    private GameObject[] _treePrefabs;
    private GameObject[] _grassPrefabs;
    private GameObject[] _bushPrefabs;
    private GameObject[] _flowerPrefabs;

    private void Awake()
    {
        // Hide the editor Ground plane — the runtime Lawn replaces it visually
        // (keep MeshCollider active so the physics floor is intact)
        GameObject editorGround = GameObject.Find("Ground");
        if (editorGround != null)
        {
            Renderer gr = editorGround.GetComponent<Renderer>();
            if (gr != null) gr.enabled = false;
        }

        LoadNatureModels();
        LoadBarkClips();
        BuildGrassMaterials();
        ConfigureLighting();
        BuildLawn();
        ScatterGrassBlades();
        BuildEnvironment();

        GameObject dog = SpawnPlayerDog("DogPlayer", Vector3.zero,
                                        new Color(0.76f, 0.54f, 0.30f));
        // Assign bark clips to the player controller
        var pdc = dog.GetComponent<PlayerDogController>();
        if (pdc != null && _barkClips != null) pdc.barkClips = _barkClips;
        var dpic = dog.GetComponent<DogPlayerInputController>();
        if (dpic != null && _barkClips != null) dpic.barkClips = _barkClips;
        SpawnBall(new Vector3(5f, 0.35f, 5f));
        ConfigureCamera(dog.transform);
        ConfigureAtmosphere();

        Debug.Log("Dog Game Ready!  WASD = move   SPACE = jump   B = bark");
    }

    // ----- Load Nature Models ------------------------------------------------
    private void LoadNatureModels()
    {
        _treePrefabs = new GameObject[]
        {
            LoadModel("Models/Trees/tree_default"),
            LoadModel("Models/Trees/tree_detailed"),
            LoadModel("Models/Trees/tree_oak"),
            LoadModel("Models/Trees/tree_fat"),
            LoadModel("Models/Trees/tree_tall"),
            LoadModel("Models/Trees/tree_pineDefaultA")
        };
        _grassPrefabs = new GameObject[]
        {
            LoadModel("Models/Grass/grass"),
            LoadModel("Models/Grass/grass_large"),
            LoadModel("Models/Grass/grass_leafs"),
            LoadModel("Models/Grass/grass_leafsLarge")
        };
        _bushPrefabs = new GameObject[]
        {
            LoadModel("Models/Bushes/plant_bush"),
            LoadModel("Models/Bushes/plant_bushDetailed"),
            LoadModel("Models/Bushes/plant_bushLarge"),
            LoadModel("Models/Bushes/plant_bushSmall")
        };
        _flowerPrefabs = new GameObject[]
        {
            LoadModel("Models/Flowers/flower_redA"),
            LoadModel("Models/Flowers/flower_yellowA"),
            LoadModel("Models/Flowers/flower_purpleA")
        };
    }

    private GameObject LoadModel(string path)
    {
        // Try loading from Resources first, then from AssetDatabase path
        GameObject go = Resources.Load<GameObject>(path);
        if (go != null) return go;
        // Fallback: load via asset path at runtime won't work, log warning
        return null;
    }

    // ----- Bark Audio -------------------------------------------------------
    private void LoadBarkClips()
    {
        _barkClips = Resources.LoadAll<AudioClip>("Audio");
        if (_barkClips == null || _barkClips.Length == 0)
            Debug.LogWarning("No bark audio clips found in Resources/Audio/. Using procedural bark.");
    }

    // ----- Materials -------------------------------------------------------
    private void BuildGrassMaterials()
    {
        _grassMats = new Material[6];
        _grassMats[0] = MakeMat(new Color(0.18f, 0.52f, 0.12f), 0.05f);
        _grassMats[1] = MakeMat(new Color(0.12f, 0.44f, 0.08f), 0.04f);
        _grassMats[2] = MakeMat(new Color(0.24f, 0.58f, 0.14f), 0.06f);
        _grassMats[3] = MakeMat(new Color(0.16f, 0.48f, 0.10f), 0.03f);
        _grassMats[4] = MakeMat(new Color(0.28f, 0.55f, 0.16f), 0.05f);
        _grassMats[5] = MakeMat(new Color(0.14f, 0.40f, 0.06f), 0.04f);
        for (int i = 0; i < _grassMats.Length; i++)
            _grassMats[i].enableInstancing = true;
    }

    // ----- Lighting --------------------------------------------------------
    private void ConfigureLighting()
    {
        GameObject sunGO = GameObject.Find("Directional Light");
        if (sunGO == null) sunGO = new GameObject("Directional Light");
        Light sun = sunGO.GetComponent<Light>();
        if (sun == null) sun = sunGO.AddComponent<Light>();

        sun.type           = LightType.Directional;
        sun.color          = new Color(1.0f, 0.95f, 0.82f);
        sun.intensity      = 0.85f;
        sun.shadows        = LightShadows.Soft;
        sun.shadowStrength = 0.80f;
        sunGO.transform.rotation = Quaternion.Euler(52f, -42f, 0f);

        RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.28f, 0.42f, 0.62f);
        RenderSettings.ambientEquatorColor = new Color(0.22f, 0.36f, 0.22f);
        RenderSettings.ambientGroundColor  = new Color(0.06f, 0.14f, 0.06f);
    }

    // ----- Lawn ------------------------------------------------------------
    private void BuildLawn()
    {
        GameObject lawn = GameObject.CreatePrimitive(PrimitiveType.Plane);
        lawn.name = "Lawn";
        lawn.transform.position   = Vector3.zero;
        lawn.transform.localScale = new Vector3(10f, 1f, 10f);
        // Standard shader for lighting interaction
        Material lawnMat = MakeMat(new Color(0.18f, 0.50f, 0.14f), 0.15f);
        Renderer lawnRend = lawn.GetComponent<Renderer>();
        lawnRend.material = lawnMat;
        lawnRend.receiveShadows = true;

        // Add subtle dirt patches near tree bases for realism
        for (int i = 0; i < 5; i++)
        {
            GameObject dirt = GameObject.CreatePrimitive(PrimitiveType.Plane);
            dirt.name = "DirtPatch";
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(14f, 17f);
            dirt.transform.position = new Vector3(
                Mathf.Cos(angle) * dist, 0.003f, Mathf.Sin(angle) * dist);
            float patchSize = Random.Range(0.04f, 0.09f);
            dirt.transform.localScale = new Vector3(patchSize, 1f, patchSize * Random.Range(0.8f, 1.2f));
            dirt.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            dirt.GetComponent<Renderer>().material =
                MakeMat(new Color(0.28f, 0.22f, 0.10f), 0.05f);
            Object.Destroy(dirt.GetComponent<MeshCollider>());
        }
    }

    private void ScatterGrassBlades()
    {
        // Grass clusters across the lawn (fewer if using 3D models)
        bool hasModels = PickRandom(_grassPrefabs) != null;
        int clusterCount = hasModels ? 180 : 350;
        int bladesPerCluster = hasModels ? 2 : 8;

        for (int i = 0; i < clusterCount; i++)
        {
            Vector3 center = new Vector3(
                Random.Range(-18f, 18f), 0f, Random.Range(-18f, 18f));
            int count = Random.Range(1, bladesPerCluster + 1);
            for (int j = 0; j < count; j++)
            {
                float ox = Random.Range(-0.6f, 0.6f);
                float oz = Random.Range(-0.6f, 0.6f);
                SpawnGrassBlade(center + new Vector3(ox, 0f, oz));
            }
        }
    }

    private void SpawnGrassBlade(Vector3 basePos)
    {
        // Use Kenney grass model if available
        GameObject prefab = PickRandom(_grassPrefabs);
        if (prefab != null)
        {
            GameObject g = Instantiate(prefab, basePos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            g.name = "Grass";
            float scale = Random.Range(0.6f, 1.5f);
            g.transform.localScale = Vector3.one * scale;
            ApplyColorToAll(g, _grassMats[Random.Range(0, _grassMats.Length)].color, Color.clear);
            // Remove colliders
            foreach (var col in g.GetComponentsInChildren<Collider>())
                Object.Destroy(col);
            foreach (var rend in g.GetComponentsInChildren<Renderer>())
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return;
        }

        // Fallback: procedural grass blade
        float h = Random.Range(0.12f, 0.45f);
        float w = Random.Range(0.012f, 0.032f);
        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Grass";
        blade.transform.position   = basePos + new Vector3(0f, h * 0.5f, 0f);
        blade.transform.localScale = new Vector3(w, h, w * 0.35f);
        blade.transform.rotation   = Quaternion.Euler(
            Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
        Object.Destroy(blade.GetComponent<BoxCollider>());
        Renderer bladeRend = blade.GetComponent<Renderer>();
        bladeRend.material = _grassMats[Random.Range(0, _grassMats.Length)];
        bladeRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ----- Environment -----------------------------------------------------
    private void BuildEnvironment()
    {
        SpawnTree(new Vector3(-16f, 0f, -16f));
        SpawnTree(new Vector3( 16f, 0f, -16f));
        SpawnTree(new Vector3(-16f, 0f,  16f));
        SpawnTree(new Vector3( 16f, 0f,  16f));

        for (int i = 0; i < 8; i++)
        {
            float sign = (Random.value < 0.5f) ? 1f : -1f;
            SpawnTree(new Vector3(
                Random.Range(-14f, 14f), 0f,
                sign * Random.Range(14f, 18f)));
        }

        // Place bushes evenly along all 4 sides of the perimeter
        for (int i = 0; i < 5; i++) // North
            SpawnBush(new Vector3(Random.Range(-16f, 16f), 0f, Random.Range(15f, 17.5f)));
        for (int i = 0; i < 5; i++) // South
            SpawnBush(new Vector3(Random.Range(-16f, 16f), 0f, Random.Range(-17.5f, -15f)));
        for (int i = 0; i < 5; i++) // East
            SpawnBush(new Vector3(Random.Range(15f, 17.5f), 0f, Random.Range(-16f, 16f)));
        for (int i = 0; i < 5; i++) // West
            SpawnBush(new Vector3(Random.Range(-17.5f, -15f), 0f, Random.Range(-16f, 16f)));

        // Scatter flowers across the lawn
        for (int i = 0; i < 30; i++)
        {
            Vector3 fpos = new Vector3(Random.Range(-16f, 16f), 0f, Random.Range(-16f, 16f));
            SpawnFlower(fpos);
        }

        SpawnFence(19f);
        SpawnAgilityObstacles();
        SpawnWallColliders(19f);
    }

    private void SpawnTree(Vector3 pos)
    {
        // Use Kenney nature model if available
        GameObject prefab = PickRandom(_treePrefabs);
        if (prefab != null)
        {
            GameObject tree = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            tree.name = "Tree";
            float scale = Random.Range(2.5f, 4.5f);
            tree.transform.localScale = Vector3.one * scale;
            ApplyColorToAll(tree, new Color(0.18f, 0.50f, 0.14f), new Color(0.35f, 0.22f, 0.10f));
            return;
        }

        // Fallback: procedural tree
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        float trunkHeight = Random.Range(2.2f, 3.2f);
        trunk.transform.position   = pos + new Vector3(0f, trunkHeight, 0f);
        trunk.transform.localScale = new Vector3(0.35f, trunkHeight, 0.35f);
        trunk.GetComponent<Renderer>().material = MakeMat(new Color(0.35f, 0.20f, 0.08f), 0.10f);

        float canopyY = trunkHeight * 2f;
        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.name = "Foliage";
        foliage.transform.position   = pos + new Vector3(0f, canopyY, 0f);
        foliage.transform.localScale = Vector3.one * 2.5f;
        foliage.GetComponent<Renderer>().material = MakeMat(new Color(0.10f, 0.38f, 0.10f), 0.08f);
        Object.Destroy(foliage.GetComponent<SphereCollider>());
    }

    private void SpawnBush(Vector3 pos)
    {
        // Use Kenney bush model if available
        GameObject prefab = PickRandom(_bushPrefabs);
        if (prefab != null)
        {
            GameObject bush = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            bush.name = "Bush";
            float scale = Random.Range(1.8f, 3.2f);
            bush.transform.localScale = Vector3.one * scale;
            ApplyColorToAll(bush, new Color(0.12f, 0.42f, 0.12f), new Color(0.28f, 0.18f, 0.08f));
            return;
        }

        // Fallback: procedural bush
        int n = Random.Range(3, 6);
        for (int i = 0; i < n; i++)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "Bush";
            float s = Random.Range(0.5f, 1.2f);
            b.transform.position = pos + new Vector3(
                Random.Range(-0.5f, 0.5f), s * 0.35f, Random.Range(-0.5f, 0.5f));
            b.transform.localScale = new Vector3(s, s * 0.55f, s * 0.9f);
            b.GetComponent<Renderer>().material =
                MakeMat(new Color(0.08f, 0.32f, 0.08f), 0.08f);
            Object.Destroy(b.GetComponent<SphereCollider>());
        }
    }

    private void SpawnFence(float half)
    {
        Material woodMat = MakeMat(new Color(0.60f, 0.40f, 0.20f), 0.18f);
        Material railMat = MakeMat(new Color(0.55f, 0.35f, 0.17f), 0.15f);

        // Posts around perimeter
        for (float t = -half; t <= half; t += 1.8f)
        {
            SpawnFencePost(new Vector3( t,  0f, -half), woodMat);
            SpawnFencePost(new Vector3( t,  0f,  half), woodMat);
            SpawnFencePost(new Vector3(-half, 0f, t),   woodMat);
            SpawnFencePost(new Vector3( half, 0f, t),   woodMat);
        }

        // Short rails along each side only (not crossing center)
        float len = half * 2f;
        // North side rails
        SpawnFenceRail(new Vector3(0f, 1.05f, -half), new Vector3(len, 0.07f, 0.07f), railMat);
        SpawnFenceRail(new Vector3(0f, 0.55f, -half), new Vector3(len, 0.07f, 0.07f), railMat);
        // South side rails
        SpawnFenceRail(new Vector3(0f, 1.05f,  half), new Vector3(len, 0.07f, 0.07f), railMat);
        SpawnFenceRail(new Vector3(0f, 0.55f,  half), new Vector3(len, 0.07f, 0.07f), railMat);
        // West side rails
        SpawnFenceRail(new Vector3(-half, 1.05f, 0f), new Vector3(0.07f, 0.07f, len), railMat);
        SpawnFenceRail(new Vector3(-half, 0.55f, 0f), new Vector3(0.07f, 0.07f, len), railMat);
        // East side rails
        SpawnFenceRail(new Vector3( half, 1.05f, 0f), new Vector3(0.07f, 0.07f, len), railMat);
        SpawnFenceRail(new Vector3( half, 0.55f, 0f), new Vector3(0.07f, 0.07f, len), railMat);
    }

    private void SpawnFencePost(Vector3 pos, Material mat)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        p.name = "FencePost";
        p.transform.position   = pos + new Vector3(0f, 0.80f, 0f);
        p.transform.localScale = new Vector3(0.12f, 1.6f, 0.12f);
        p.GetComponent<Renderer>().material = mat;
    }

    private void SpawnFenceRail(Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.name = "FenceRail";
        r.transform.position   = pos;
        r.transform.localScale = scale;
        r.GetComponent<Renderer>().material = mat;
    }

    // ----- Dog Agility Obstacles -------------------------------------------
    private void SpawnAgilityObstacles()
    {
        Material poleMat  = MakeMat(new Color(0.85f, 0.85f, 0.85f), 0.50f);
        Material barMat   = MakeMat(new Color(0.90f, 0.20f, 0.20f), 0.40f);
        Material barMat2  = MakeMat(new Color(0.20f, 0.40f, 0.90f), 0.40f);
        Material rampMat  = MakeMat(new Color(0.90f, 0.75f, 0.20f), 0.30f);
        Material tunnelMat = MakeMat(new Color(0.30f, 0.55f, 0.85f), 0.35f);
        Material ringMat  = MakeMat(new Color(0.90f, 0.50f, 0.10f), 0.40f);

        // --- Hurdle jumps (FCI standard heights) ---
        SpawnHurdle(new Vector3(-6f, 0f, -4f), 0f, 0.40f, barMat, poleMat);  // small
        SpawnHurdle(new Vector3( 0f, 0f, -4f), 0f, 0.55f, barMat2, poleMat); // medium
        SpawnHurdle(new Vector3( 6f, 0f, -4f), 0f, 0.65f, barMat, poleMat);  // large

        // --- Weave poles (0.60m spacing, FCI standard) ---
        SpawnWeavePoles(new Vector3(-8f, 0f, 5f), 8, poleMat);

        // --- A-Frame ramp ---
        SpawnAFrame(new Vector3(6f, 0f, 6f), 0f, rampMat);

        // --- Tunnel ---
        SpawnTunnel(new Vector3(-2f, 0f, 10f), 90f, tunnelMat);

        // --- Tire jump (ring) ---
        SpawnTireJump(new Vector3(10f, 0f, 0f), 0f, ringMat, poleMat);
    }

    private void SpawnHurdle(Vector3 pos, float yRot, float barHeight, Material barMat, Material poleMat)
    {
        GameObject parent = new GameObject("Hurdle");
        parent.transform.position = pos;
        parent.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        // Two upright poles
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "HurdlePole";
            pole.transform.SetParent(parent.transform);
            pole.transform.localPosition = new Vector3(side * 1.2f, 0.6f, 0f);
            pole.transform.localScale    = new Vector3(0.08f, 0.6f, 0.08f);
            pole.GetComponent<Renderer>().material = poleMat;
        }

        // Horizontal bar (the jump bar)
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bar.name = "HurdleBar";
        bar.transform.SetParent(parent.transform);
        bar.transform.localPosition = new Vector3(0f, barHeight, 0f);
        bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        bar.transform.localScale    = new Vector3(0.06f, 1.2f, 0.06f);
        bar.GetComponent<Renderer>().material = barMat;

        // Optional: second bar lower
        if (barHeight > 0.6f)
        {
            GameObject bar2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar2.name = "HurdleBar2";
            bar2.transform.SetParent(parent.transform);
            bar2.transform.localPosition = new Vector3(0f, barHeight * 0.5f, 0f);
            bar2.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            bar2.transform.localScale    = new Vector3(0.05f, 1.2f, 0.05f);
            bar2.GetComponent<Renderer>().material = barMat;
        }
    }

    private void SpawnWeavePoles(Vector3 startPos, int count, Material mat)
    {
        GameObject parent = new GameObject("WeavePoles");
        parent.transform.position = startPos;

        for (int i = 0; i < count; i++)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "WeavePole";
            pole.transform.SetParent(parent.transform);
            pole.transform.localPosition = new Vector3(i * 0.60f, 0.55f, 0f);
            pole.transform.localScale    = new Vector3(0.08f, 0.55f, 0.08f);
            // Alternate colors
            Material poleMat = (i % 2 == 0)
                ? MakeMat(new Color(0.90f, 0.20f, 0.20f), 0.45f)
                : MakeMat(new Color(0.85f, 0.85f, 0.85f), 0.50f);
            pole.GetComponent<Renderer>().material = poleMat;
        }
    }

    private void SpawnAFrame(Vector3 pos, float yRot, Material mat)
    {
        GameObject parent = new GameObject("AFrame");
        parent.transform.position = pos;
        parent.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        // Left ramp going up
        GameObject rampUp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampUp.name = "RampUp";
        rampUp.transform.SetParent(parent.transform);
        rampUp.transform.localPosition = new Vector3(0f, 0.7f, -1.2f);
        rampUp.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
        rampUp.transform.localScale    = new Vector3(1.2f, 0.08f, 2.8f);
        rampUp.GetComponent<Renderer>().material = mat;

        // Right ramp going down
        GameObject rampDown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampDown.name = "RampDown";
        rampDown.transform.SetParent(parent.transform);
        rampDown.transform.localPosition = new Vector3(0f, 0.7f, 1.2f);
        rampDown.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
        rampDown.transform.localScale    = new Vector3(1.2f, 0.08f, 2.8f);
        rampDown.GetComponent<Renderer>().material = mat;

        // Contact zone strips (darker yellow at bottom of ramps)
        Material contactMat = MakeMat(new Color(0.85f, 0.55f, 0.10f), 0.25f);
        GameObject cz1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cz1.name = "ContactZone";
        cz1.transform.SetParent(rampUp.transform);
        cz1.transform.localPosition = new Vector3(0f, 0.01f, -0.35f);
        cz1.transform.localScale    = new Vector3(0.95f, 1.2f, 0.3f);
        cz1.GetComponent<Renderer>().material = contactMat;
        Object.Destroy(cz1.GetComponent<BoxCollider>());

        GameObject cz2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cz2.name = "ContactZone";
        cz2.transform.SetParent(rampDown.transform);
        cz2.transform.localPosition = new Vector3(0f, 0.01f, 0.35f);
        cz2.transform.localScale    = new Vector3(0.95f, 1.2f, 0.3f);
        cz2.GetComponent<Renderer>().material = contactMat;
        Object.Destroy(cz2.GetComponent<BoxCollider>());
    }

    private void SpawnTunnel(Vector3 pos, float yRot, Material mat)
    {
        GameObject parent = new GameObject("Tunnel");
        parent.transform.position = pos;
        parent.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        float tunnelRadius = 0.65f;
        float tunnelLength = 4.0f;
        int   rings        = 12;       // number of cross-section rings
        int   staves       = 14;       // cubes per ring (upper arch only)
        float staveThick   = 0.08f;

        Material darkMat = MakeMat(new Color(0.22f, 0.42f, 0.72f), 0.35f);
        Material lightMat = MakeMat(new Color(0.35f, 0.58f, 0.88f), 0.35f);

        for (int r = 0; r < rings; r++)
        {
            float z = Mathf.Lerp(-tunnelLength * 0.5f, tunnelLength * 0.5f, (float)r / (rings - 1));
            Material ringMat = (r % 2 == 0) ? mat : darkMat;

            // Build an arch from cubes (skip the bottom so dog walks through)
            for (int s = 0; s < staves; s++)
            {
                // Angle from -30 deg to 210 deg (arch over the top, open at bottom)
                float angle = Mathf.Lerp(-20f, 200f, (float)s / (staves - 1)) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * tunnelRadius;
                float y = Mathf.Sin(angle) * tunnelRadius + tunnelRadius * 0.3f;

                GameObject stave = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stave.name = "TunnelStave";
                stave.transform.SetParent(parent.transform);
                stave.transform.localPosition = new Vector3(x, y, z);
                stave.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                stave.transform.localScale    = new Vector3(staveThick, staveThick, tunnelLength / rings * 1.1f);

                // Alternate colors for visual interest
                stave.GetComponent<Renderer>().material = (s % 2 == 0) ? ringMat : lightMat;

                // Remove colliders from top arch staves so dog can walk through
                // Only keep colliders on the side/bottom staves for structure
                if (angle > 0.5f && angle < 2.6f)
                    Object.Destroy(stave.GetComponent<BoxCollider>());
            }
        }

        // Entrance/exit ring frames (thicker arches at each end)
        Material frameMat = MakeMat(new Color(0.18f, 0.35f, 0.65f), 0.45f);
        for (int end = -1; end <= 1; end += 2)
        {
            float z = end * tunnelLength * 0.5f;
            for (int s = 0; s < 20; s++)
            {
                float angle = Mathf.Lerp(-30f, 210f, (float)s / 19f) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * (tunnelRadius + 0.06f);
                float y = Mathf.Sin(angle) * (tunnelRadius + 0.06f) + tunnelRadius * 0.3f;

                GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frame.name = "TunnelFrame";
                frame.transform.SetParent(parent.transform);
                frame.transform.localPosition = new Vector3(x, y, z);
                frame.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                frame.transform.localScale    = new Vector3(0.12f, 0.12f, 0.12f);
                frame.GetComponent<Renderer>().material = frameMat;
                Object.Destroy(frame.GetComponent<BoxCollider>());
            }
        }
    }

    private void SpawnTireJump(Vector3 pos, float yRot, Material ringMat, Material poleMat)
    {
        GameObject parent = new GameObject("TireJump");
        parent.transform.position = pos;
        parent.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        // Frame - two tall poles
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "TireFrame";
            pole.transform.SetParent(parent.transform);
            pole.transform.localPosition = new Vector3(side * 1.0f, 1.0f, 0f);
            pole.transform.localScale    = new Vector3(0.10f, 1.0f, 0.10f);
            pole.GetComponent<Renderer>().material = poleMat;
        }

        // Top crossbar
        GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crossbar.name = "TireCrossbar";
        crossbar.transform.SetParent(parent.transform);
        crossbar.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        crossbar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        crossbar.transform.localScale    = new Vector3(0.08f, 1.0f, 0.08f);
        crossbar.GetComponent<Renderer>().material = poleMat;

        // Hollow tire ring built from small cubes arranged in a circle
        float tireRadius = 0.45f;
        float tireCenter = 1.2f;
        int segments = 24;
        float segSize = 0.10f;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float ty = Mathf.Sin(angle) * tireRadius + tireCenter;
            float tx = Mathf.Cos(angle) * tireRadius;

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "TireSegment";
            seg.transform.SetParent(parent.transform);
            seg.transform.localPosition = new Vector3(tx, ty, 0f);
            seg.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            seg.transform.localScale    = new Vector3(segSize, segSize, segSize);
            seg.GetComponent<Renderer>().material = ringMat;
            Object.Destroy(seg.GetComponent<BoxCollider>());
        }
    }

    private void SpawnFlower(Vector3 pos)
    {
        GameObject prefab = PickRandom(_flowerPrefabs);
        if (prefab == null) return;
        GameObject flower = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        flower.name = "Flower";
        flower.transform.localScale = Vector3.one * Random.Range(1.2f, 2.0f);
        foreach (var col in flower.GetComponentsInChildren<Collider>())
            Object.Destroy(col);
    }

    private static GameObject PickRandom(GameObject[] arr)
    {
        if (arr == null || arr.Length == 0) return null;
        // Filter nulls
        var valid = new System.Collections.Generic.List<GameObject>();
        foreach (var g in arr) if (g != null) valid.Add(g);
        return valid.Count > 0 ? valid[Random.Range(0, valid.Count)] : null;
    }

    private static void ApplyColorToAll(GameObject root, Color leafColor, Color trunkColor)
    {
        foreach (Renderer rend in root.GetComponentsInChildren<Renderer>())
        {
            if (rend.sharedMaterial != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                // Heuristic: if the mesh name contains "trunk" or "bark", use trunk color
                string meshName = rend.gameObject.name.ToLower();
                if (trunkColor != Color.clear && (meshName.Contains("trunk") || meshName.Contains("bark") || meshName.Contains("wood")))
                {
                    mat.color = trunkColor;
                    mat.SetFloat("_Glossiness", 0.10f);
                }
                else
                {
                    mat.color = leafColor;
                    mat.SetFloat("_Glossiness", 0.08f);
                }
                mat.SetFloat("_Metallic", 0f);
                rend.material = mat;
            }
        }
    }

    // ----- Wall Colliders (invisible barriers on all 4 sides) ---------------
    private void SpawnWallColliders(float half)
    {
        float wallHeight = 3f;
        float wallThick  = 0.5f;
        float len        = half * 2f + wallThick;

        // North wall
        SpawnInvisibleWall("WallNorth", new Vector3(0f, wallHeight * 0.5f,  half), new Vector3(len, wallHeight, wallThick));
        // South wall
        SpawnInvisibleWall("WallSouth", new Vector3(0f, wallHeight * 0.5f, -half), new Vector3(len, wallHeight, wallThick));
        // East wall
        SpawnInvisibleWall("WallEast",  new Vector3( half, wallHeight * 0.5f, 0f), new Vector3(wallThick, wallHeight, len));
        // West wall
        SpawnInvisibleWall("WallWest",  new Vector3(-half, wallHeight * 0.5f, 0f), new Vector3(wallThick, wallHeight, len));
    }

    private void SpawnInvisibleWall(string wallName, Vector3 pos, Vector3 scale)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.position = pos;
        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = scale;
    }

    // ----- Player Dog ------------------------------------------------------
    private GameObject SpawnPlayerDog(string dogName, Vector3 position, Color furColor)
    {
        // Use ithappy Dog_001 prefab if assigned via inspector
        if (dogPrefab != null)
            return SpawnPrefabDog(dogName, position);

        // Try loading the German Shepherd model
        GameObject gsDog = SpawnGermanShepherd(dogName, position);
        if (gsDog != null) return gsDog;

        // Fallback: procedural sphere-based dog
        GameObject root = new GameObject(dogName);
        root.transform.position = position;

        Material fur      = MakeMat(furColor, 0.10f);
        Material darkFur  = MakeMat(furColor * 0.70f, 0.09f);
        Color bc = new Color(
            Mathf.Min(furColor.r + 0.08f, 1f),
            Mathf.Min(furColor.g + 0.06f, 1f),
            Mathf.Min(furColor.b + 0.04f, 1f));
        Material bellyFur = MakeMat(bc, 0.09f);
        Material noseMat  = MakeMat(new Color(0.04f, 0.04f, 0.04f), 0.85f);
        noseMat.SetFloat("_Metallic", 0.15f);
        Material eyeWhiteMat = MakeMat(Color.white, 0.62f);
        Material eyeIrisMat  = MakeMat(new Color(0.18f, 0.11f, 0.04f), 0.55f);
        Material eyePupilMat = MakeMat(Color.black, 0.80f);
        Material tongueMat   = MakeMat(new Color(0.92f, 0.35f, 0.40f), 0.45f);

        AddDogPart("Body", root, PrimitiveType.Sphere,
            new Vector3(0f, 0.88f, 0f), new Vector3(0.65f, 0.50f, 1.22f), fur);
        AddDogPart("Belly", root, PrimitiveType.Sphere,
            new Vector3(0f, 0.67f, 0f), new Vector3(0.50f, 0.27f, 0.90f), bellyFur);

        AddDogPart("FrontLeftLeg", root, PrimitiveType.Cylinder,
            new Vector3(-0.22f, 0.32f,  0.38f), new Vector3(0.13f, 0.32f, 0.13f), fur);
        AddDogPart("FrontLeftLegPaw", root, PrimitiveType.Sphere,
            new Vector3(-0.22f, 0.06f, 0.45f), new Vector3(0.20f, 0.09f, 0.25f), darkFur);
        AddDogPart("FrontRightLeg", root, PrimitiveType.Cylinder,
            new Vector3( 0.22f, 0.32f,  0.38f), new Vector3(0.13f, 0.32f, 0.13f), fur);
        AddDogPart("FrontRightLegPaw", root, PrimitiveType.Sphere,
            new Vector3( 0.22f, 0.06f, 0.45f), new Vector3(0.20f, 0.09f, 0.25f), darkFur);
        AddDogPart("BackLeftLeg", root, PrimitiveType.Cylinder,
            new Vector3(-0.22f, 0.32f, -0.36f), new Vector3(0.13f, 0.32f, 0.13f), fur);
        AddDogPart("BackLeftLegPaw", root, PrimitiveType.Sphere,
            new Vector3(-0.22f, 0.06f, -0.43f), new Vector3(0.20f, 0.09f, 0.25f), darkFur);
        AddDogPart("BackRightLeg", root, PrimitiveType.Cylinder,
            new Vector3( 0.22f, 0.32f, -0.36f), new Vector3(0.13f, 0.32f, 0.13f), fur);
        AddDogPart("BackRightLegPaw", root, PrimitiveType.Sphere,
            new Vector3( 0.22f, 0.06f, -0.43f), new Vector3(0.20f, 0.09f, 0.25f), darkFur);

        AddDogPart("Neck", root, PrimitiveType.Sphere,
            new Vector3(0f, 1.02f, 0.46f), new Vector3(0.30f, 0.34f, 0.32f), fur);
        AddDogPart("Head", root, PrimitiveType.Sphere,
            new Vector3(0f, 1.12f, 0.68f), new Vector3(0.50f, 0.47f, 0.50f), fur);
        AddDogPart("Snout", root, PrimitiveType.Sphere,
            new Vector3(0f, 0.99f, 1.01f), new Vector3(0.28f, 0.20f, 0.38f), fur);

        GameObject lowerJaw = AddDogPart("LowerJaw", root, PrimitiveType.Sphere,
            new Vector3(0f, 0.92f, 1.03f), new Vector3(0.24f, 0.12f, 0.30f), darkFur);
        AddDogPart("Tongue", lowerJaw, PrimitiveType.Sphere,
            new Vector3(0f, -0.02f, 0.10f), new Vector3(0.14f, 0.04f, 0.18f), tongueMat);

        AddDogPart("Nose", root, PrimitiveType.Sphere,
            new Vector3(0f, 0.99f, 1.20f), new Vector3(0.12f, 0.10f, 0.08f), noseMat);

        GameObject leftEar = AddDogPart("LeftEar", root, PrimitiveType.Sphere,
            new Vector3(-0.30f, 1.28f, 0.65f), new Vector3(0.14f, 0.40f, 0.09f), darkFur);
        leftEar.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
        GameObject rightEar = AddDogPart("RightEar", root, PrimitiveType.Sphere,
            new Vector3( 0.30f, 1.28f, 0.65f), new Vector3(0.14f, 0.40f, 0.09f), darkFur);
        rightEar.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);

        AddDogPart("LeftEyeWhite", root, PrimitiveType.Sphere,
            new Vector3(-0.185f, 1.16f, 0.920f), new Vector3(0.10f, 0.10f, 0.05f), eyeWhiteMat);
        AddDogPart("LeftEyeIris", root, PrimitiveType.Sphere,
            new Vector3(-0.185f, 1.16f, 0.938f), new Vector3(0.07f, 0.07f, 0.04f), eyeIrisMat);
        AddDogPart("LeftEyePupil", root, PrimitiveType.Sphere,
            new Vector3(-0.185f, 1.16f, 0.958f), new Vector3(0.04f, 0.05f, 0.03f), eyePupilMat);
        AddDogPart("LeftEyeLid", root, PrimitiveType.Sphere,
            new Vector3(-0.185f, 1.18f, 0.910f), new Vector3(0.10f, 0.045f, 0.04f), darkFur);

        AddDogPart("RightEyeWhite", root, PrimitiveType.Sphere,
            new Vector3( 0.185f, 1.16f, 0.920f), new Vector3(0.10f, 0.10f, 0.05f), eyeWhiteMat);
        AddDogPart("RightEyeIris", root, PrimitiveType.Sphere,
            new Vector3( 0.185f, 1.16f, 0.938f), new Vector3(0.07f, 0.07f, 0.04f), eyeIrisMat);
        AddDogPart("RightEyePupil", root, PrimitiveType.Sphere,
            new Vector3( 0.185f, 1.16f, 0.958f), new Vector3(0.04f, 0.05f, 0.03f), eyePupilMat);
        AddDogPart("RightEyeLid", root, PrimitiveType.Sphere,
            new Vector3( 0.185f, 1.18f, 0.910f), new Vector3(0.10f, 0.045f, 0.04f), darkFur);

        GameObject tail = AddDogPart("Tail", root, PrimitiveType.Cylinder,
            new Vector3(0f, 1.04f, -0.64f), new Vector3(0.07f, 0.24f, 0.07f), darkFur);
        tail.transform.localRotation = Quaternion.Euler(-52f, 0f, 0f);

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.center          = new Vector3(0f, 0.70f, 0f);
        cc.height          = 1.40f;
        cc.radius          = 0.38f;
        cc.skinWidth       = 0.04f;
        cc.minMoveDistance = 0f;

        AudioSource audio = root.AddComponent<AudioSource>();
        audio.spatialBlend = 0.8f;
        audio.rolloffMode  = AudioRolloffMode.Linear;
        audio.maxDistance  = 25f;

        root.AddComponent<PlayerDogController>();
        return root;
    }

    private GameObject SpawnGermanShepherd(string dogName, Vector3 position)
    {
        // Try multiple paths to find the German Shepherd model
        GameObject prefab = Resources.Load<GameObject>("Models/Dog/SK_GermanShepherd_01");
        if (prefab == null)
        {
            // Try loading all GameObjects in the Dog folder
            GameObject[] allDogs = Resources.LoadAll<GameObject>("Models/Dog");
            foreach (var d in allDogs)
            {
                if (d.name.Contains("German") || d.name.Contains("Shepherd") || d.name.Contains("SK_"))
                {
                    prefab = d;
                    break;
                }
            }
        }
        if (prefab == null)
        {
            Debug.LogWarning("German Shepherd model not found in Resources. Falling back.");
            return null;
        }

        GameObject dog = Instantiate(prefab, position, Quaternion.identity);
        dog.name = dogName;

        // Create a Standard shader material from the German Shepherd texture
        Texture2D baseTex  = Resources.Load<Texture2D>("Models/Dog/T_GermanShepherd_B");
        Texture2D normalTex = Resources.Load<Texture2D>("Models/Dog/T_GermanShepherd_N");
        Material dogMat = new Material(Shader.Find("Standard"));
        if (baseTex != null)
        {
            dogMat.mainTexture = baseTex;
            dogMat.color = Color.white;
        }
        if (normalTex != null)
        {
            dogMat.EnableKeyword("_NORMALMAP");
            dogMat.SetTexture("_BumpMap", normalTex);
        }
        dogMat.SetFloat("_Glossiness", 0.25f);
        dogMat.SetFloat("_Metallic", 0f);

        // Apply material to all renderers
        foreach (Renderer rend in dog.GetComponentsInChildren<Renderer>())
            rend.material = dogMat;

        // Assign the gameplay animator controller (Idle/Walk/Run driven by Speed)
        RuntimeAnimatorController ac = Resources.Load<RuntimeAnimatorController>("Models/Dog/AC_GS_Gameplay");
        if (ac == null) ac = Resources.Load<RuntimeAnimatorController>("Models/Dog/AC_Dogs_Type_01");
        Animator animator = dog.GetComponent<Animator>();
        if (animator == null) animator = dog.AddComponent<Animator>();
        if (ac != null) animator.runtimeAnimatorController = ac;

        // Add CharacterController sized for German Shepherd
        CharacterController cc = dog.AddComponent<CharacterController>();
        cc.center          = new Vector3(0f, 0.45f, 0f);
        cc.height          = 0.9f;
        cc.radius          = 0.25f;
        cc.skinWidth       = 0.04f;
        cc.minMoveDistance = 0f;

        // Add audio
        AudioSource audio = dog.AddComponent<AudioSource>();
        audio.spatialBlend = 0f;
        audio.volume       = 1f;

        // Add player controller
        dog.AddComponent<PlayerDogController>();

        Debug.Log("German Shepherd loaded successfully!");
        return dog;
    }

    private GameObject SpawnPrefabDog(string dogName, Vector3 position)
    {
        GameObject dog = Instantiate(dogPrefab, position, Quaternion.identity);
        dog.name = dogName;

        // Configure CharacterController to match the Dog_001 model size
        CharacterController cc = dog.GetComponent<CharacterController>();
        if (cc == null) cc = dog.AddComponent<CharacterController>();
        cc.center          = new Vector3(0f, 0.5f, 0f);
        cc.height          = 1.0f;
        cc.radius          = 0.3f;
        cc.skinWidth       = 0.04f;
        cc.minMoveDistance = 0f;

        // Add our player input + bark controller
        // (Its Awake removes ithappy's MovePlayerInput & CreatureMover)
        dog.AddComponent<DogPlayerInputController>();

        return dog;
    }

    // ----- Ball ------------------------------------------------------------
    private void SpawnBall(Vector3 position)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Ball";
        ball.transform.position   = position;
        ball.transform.localScale = Vector3.one * 0.35f;

        Material mat = MakeMat(new Color(0.92f, 0.22f, 0.22f), 0.85f);
        mat.SetFloat("_Metallic", 0.05f);
        ball.GetComponent<Renderer>().material = mat;

        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.mass        = 0.058f;   // tennis ball mass (kg)
        rb.linearDamping        = 0.47f;    // sphere air drag
        rb.angularDamping = 0.3f;

        PhysicsMaterial bm = new PhysicsMaterial("BallPhys");
        bm.bounciness      = 0.65f;   // grass bounce
        bm.dynamicFriction = 0.50f;
        bm.staticFriction  = 0.60f;
        bm.frictionCombine = PhysicsMaterialCombine.Average;
        bm.bounceCombine   = PhysicsMaterialCombine.Average;
        ball.GetComponent<SphereCollider>().material = bm;

        ball.AddComponent<BallController>();
    }

    // ----- Camera ----------------------------------------------------------
    private void ConfigureCamera(Transform dogTransform)
    {
        Camera cam = Camera.main;
        GameObject camGO = (cam != null) ? cam.gameObject : new GameObject("MainCamera");
        if (camGO.GetComponent<Camera>() == null)
        {
            camGO.AddComponent<Camera>().tag = "MainCamera";
            if (camGO.GetComponent<AudioListener>() == null)
                camGO.AddComponent<AudioListener>();
        }

        camGO.transform.position = dogTransform.position + new Vector3(0f, 4f, -9f);
        camGO.transform.rotation = Quaternion.Euler(22f, 0f, 0f);

        CameraFollow follow = camGO.GetComponent<CameraFollow>();
        if (follow == null) follow = camGO.AddComponent<CameraFollow>();
        follow.target      = dogTransform;
        follow.distance    = 8f;
        follow.height      = 5f;
        follow.smoothSpeed = 5f;
    }

    // ----- Atmosphere ------------------------------------------------------
    private void ConfigureAtmosphere()
    {
        RenderSettings.fog              = true;
        RenderSettings.fogColor         = new Color(0.68f, 0.82f, 0.72f);
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance   = 90f;
    }

    // ----- Helpers ---------------------------------------------------------
    private static GameObject AddDogPart(string nm, GameObject parent, PrimitiveType prim,
        Vector3 lPos, Vector3 lScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(prim);
        go.name = nm;
        go.transform.SetParent(parent.transform, worldPositionStays: false);
        go.transform.localPosition = lPos;
        go.transform.localScale    = lScale;
        go.GetComponent<Renderer>().material = mat;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        return go;
    }

    private static Material MakeMat(Color color, float smoothness)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        return mat;
    }
}
