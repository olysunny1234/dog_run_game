using UnityEngine;

public class DogMaterials : MonoBehaviour
{
    private void Start()
    {
        // Create and apply materials to dogs
        SetupDogAppearance(GameObject.Find("Dog1"));
        SetupDogAppearance(GameObject.Find("Dog2"));
    }

    private void SetupDogAppearance(GameObject dog)
    {
        if (dog == null) return;

        // Set body material
        Renderer bodyRenderer = dog.GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            Material bodyMaterial = new Material(Shader.Find("Standard"));
            bodyMaterial.color = dog.name == "Dog1" ? new Color(0.8f, 0.6f, 0.4f) : new Color(1f, 0.8f, 0.2f);
            bodyRenderer.material = bodyMaterial;
        }

        // Make the dog look more dog-like
        dog.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f); // Slightly taller, narrower

        // Add visual features
        CreateDogFeatures(dog);
    }

    private void CreateDogFeatures(GameObject dog)
    {
        Color dogColor = dog.name == "Dog1" ? new Color(0.8f, 0.6f, 0.4f) : new Color(1f, 0.8f, 0.2f);

        // Create ears (small spheres on top)
        GameObject leftEar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEar.name = "LeftEar";
        leftEar.transform.parent = dog.transform;
        leftEar.transform.localPosition = new Vector3(-0.3f, 0.8f, 0.2f);
        leftEar.transform.localScale = new Vector3(0.2f, 0.4f, 0.1f);
        leftEar.GetComponent<Renderer>().material.color = dogColor * 0.8f;

        GameObject rightEar = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEar.name = "RightEar";
        rightEar.transform.parent = dog.transform;
        rightEar.transform.localPosition = new Vector3(0.3f, 0.8f, 0.2f);
        rightEar.transform.localScale = new Vector3(0.2f, 0.4f, 0.1f);
        rightEar.GetComponent<Renderer>().material.color = dogColor * 0.8f;

        // Create a tail (small cylinder)
        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tail.name = "Tail";
        tail.transform.parent = dog.transform;
        tail.transform.localPosition = new Vector3(0f, 0.2f, -0.6f);
        tail.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
        tail.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        tail.GetComponent<Renderer>().material.color = dogColor * 0.9f;

        // Create eyes (small black spheres)
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

        // Create a nose (small black cube)
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.parent = dog.transform;
        nose.transform.localPosition = new Vector3(0f, 0.1f, 0.5f);
        nose.transform.localScale = new Vector3(0.1f, 0.05f, 0.05f);
        nose.GetComponent<Renderer>().material.color = Color.black;
    }
}