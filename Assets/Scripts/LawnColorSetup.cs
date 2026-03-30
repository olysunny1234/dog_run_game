using UnityEngine;

public class LawnColorSetup : MonoBehaviour
{
    private void Start()
    {
        SetupColors();
    }

    private void SetupColors()
    {
        // Setup lawn ground
        GameObject lawn = GameObject.Find("LawnGround");
        if (lawn != null)
        {
            Renderer renderer = lawn.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.2f, 0.6f, 0.2f); // Rich green lawn
                renderer.material = material;
            }
        }

        // Setup dogs
        SetupDogColors();

        // Setup trees
        SetupTreeColors();

        // Setup bushes
        SetupBushColors();

        Debug.Log("Lawn colors setup complete!");
    }

    private void SetupDogColors()
    {
        // Dog1 - brown
        GameObject dog1 = GameObject.Find("Dog1");
        if (dog1 != null)
        {
            Renderer renderer = dog1.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.8f, 0.6f, 0.4f);
                renderer.material = material;
            }
        }

        // Dog2 - golden
        GameObject dog2 = GameObject.Find("Dog2");
        if (dog2 != null)
        {
            Renderer renderer = dog2.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(1f, 0.8f, 0.2f);
                renderer.material = material;
            }
        }
    }

    private void SetupTreeColors()
    {
        // Tree trunks - brown
        GameObject[] trunks = { GameObject.Find("Tree1"), GameObject.Find("Tree2") };
        foreach (GameObject trunk in trunks)
        {
            if (trunk != null)
            {
                Renderer renderer = trunk.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = new Material(Shader.Find("Standard"));
                    material.color = new Color(0.4f, 0.2f, 0.1f);
                    renderer.material = material;
                }
            }
        }

        // Tree foliage - green
        GameObject[] foliage = { GameObject.Find("Tree1Foliage"), GameObject.Find("Tree2Foliage") };
        foreach (GameObject leaf in foliage)
        {
            if (leaf != null)
            {
                Renderer renderer = leaf.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = new Material(Shader.Find("Standard"));
                    material.color = new Color(0.1f, 0.4f, 0.1f);
                    renderer.material = material;
                }
            }
        }
    }

    private void SetupBushColors()
    {
        GameObject[] bushes = { GameObject.Find("Bush1"), GameObject.Find("Bush2") };
        foreach (GameObject bush in bushes)
        {
            if (bush != null)
            {
                Renderer renderer = bush.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material material = new Material(Shader.Find("Standard"));
                    material.color = new Color(0.15f, 0.5f, 0.15f);
                    renderer.material = material;
                }
            }
        }
    }
}