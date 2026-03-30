using UnityEngine;

public class TestBark : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("TestBark script started!");
        Dog dog = GetComponent<Dog>();
        if (dog != null)
        {
            Debug.Log("Found Dog component, calling Bark()");
            // Use reflection to call the private Bark method
            System.Reflection.MethodInfo barkMethod = typeof(Dog).GetMethod("Bark", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (barkMethod != null)
            {
                barkMethod.Invoke(dog, null);
            }
        }
        else
        {
            Debug.Log("No Dog component found!");
        }
    }
}