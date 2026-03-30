using UnityEngine;

public class BarkingDogsScene : MonoBehaviour
{
    private void Awake()
    {
        SetupScene();
    }

    private void SetupScene()
    {
        // Create dogs if they don't exist
        CreateDog("Dog1", new Vector3(-5, 0.5f, 0), new Color(0.8f, 0.6f, 0.4f));
        CreateDog("Dog2", new Vector3(5, 0.5f, 0), new Color(1f, 0.8f, 0.2f));

        // Create ground
        CreateGround();

        // Setup camera and lighting
        SetupCameraAndLighting();
    }

    private void CreateDog(string name, Vector3 position, Color color)
    {
        GameObject dog = GameObject.Find(name);
        if (dog == null)
        {
            dog = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dog.name = name;
        }

        dog.transform.position = position;
        dog.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);

        // Add components
        AudioSource audioSource = dog.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = dog.AddComponent<AudioSource>();
        }

        Dog dogScript = dog.GetComponent<Dog>();
        if (dogScript == null)
        {
            dog.AddComponent<Dog>();
        }

        // Setup appearance
        SetupDogAppearance(dog, color);
    }

    private void SetupDogAppearance(GameObject dog, Color color)
    {
        // Set body material
        Renderer renderer = dog.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.material = material;
        }

        // Add features
        CreateDogFeatures(dog, color);
    }

    private void CreateDogFeatures(GameObject dog, Color color)
    {
        // Create ears
        GameObject leftEar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEar.name = "LeftEar";
        leftEar.transform.parent = dog.transform;
        leftEar.transform.localPosition = new Vector3(-0.3f, 0.8f, 0.2f);
        leftEar.transform.localScale = new Vector3(0.2f, 0.4f, 0.1f);
        leftEar.GetComponent<Renderer>().material.color = color * 0.8f;

        GameObject rightEar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEar.name = "RightEar";
        rightEar.transform.parent = dog.transform;
        rightEar.transform.localPosition = new Vector3(0.3f, 0.8f, 0.2f);
        rightEar.transform.localScale = new Vector3(0.2f, 0.4f, 0.1f);
        rightEar.GetComponent<Renderer>().material.color = color * 0.8f;

        // Create tail
        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tail.name = "Tail";
        tail.transform.parent = dog.transform;
        tail.transform.localPosition = new Vector3(0f, 0.2f, -0.6f);
        tail.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
        tail.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        tail.GetComponent<Renderer>().material.color = color * 0.9f;

        // Create eyes
        GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEye.name = "LeftEye";
        leftEye.transform.parent = dog.transform;
        leftEye.transform.localPosition = new Vector3(-0.15f, 0.4f, 0.45f);
        leftEye.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        leftEye.GetComponent<Renderer>().material.color = Color.black;

        GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEye.name = "RightEye";
        rightEye.transform.parent = dog.transform;
        rightEye.transform.localPosition = new Vector3(0.15f, 0.4f, 0.45f);
        rightEye.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        rightEye.GetComponent<Renderer>().material.color = Color.black;

        // Create nose
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.parent = dog.transform;
        nose.transform.localPosition = new Vector3(0f, 0.1f, 0.5f);
        nose.transform.localScale = new Vector3(0.1f, 0.05f, 0.05f);
        nose.GetComponent<Renderer>().material.color = Color.black;
    }

    private void CreateGround()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
        }

        ground.transform.position = new Vector3(0, -0.3f, 0);
        ground.transform.localScale = new Vector3(20, 0.5f, 20);

        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.4f, 0.8f, 0.4f); // Green ground
            renderer.material = material;
        }
    }

    private void SetupCameraAndLighting()
    {
        // Setup camera
        GameObject cameraObj = GameObject.Find("MainCamera");
        if (cameraObj == null)
        {
            cameraObj = new GameObject("MainCamera");
            cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
        }

        cameraObj.transform.position = new Vector3(0, 1.5f, -5);
        cameraObj.transform.LookAt(new Vector3(0, 0.5f, 0));

        // Setup lighting
        GameObject lightObj = GameObject.Find("DirectionalLight");
        if (lightObj == null)
        {
            lightObj = new GameObject("DirectionalLight");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        lightObj.transform.position = new Vector3(0, 5, 5);
        lightObj.transform.rotation = Quaternion.Euler(45, -45, 0);
    }
}