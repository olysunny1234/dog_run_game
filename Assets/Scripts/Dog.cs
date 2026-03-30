using UnityEngine;
using System.Collections;

public class Dog : MonoBehaviour
{
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float barkCooldown = 2f;
    [SerializeField] private AudioClip[] barkSounds;
    
    private AudioSource audioSource;
    private float lastBarkTime = 0f;
    private Dog otherDog;

    // Tail wag
    private Transform _tail;
    private Quaternion _tailRestRot;
    private const float WagSpeed      = 4.5f;  // Hz
    private const float WagAmplitude  = 28f;    // degrees
    private float _wagOffset;                   // phase offset so dogs aren't in sync

    private void Start()
    {
        _tail = transform.Find("Tail");
        if (_tail != null)
        {
            _tailRestRot = _tail.localRotation;
            _wagOffset   = Random.Range(0f, Mathf.PI * 2f);
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Load real bark clips from Resources if none assigned
        if (barkSounds == null || barkSounds.Length == 0)
        {
            barkSounds = Resources.LoadAll<AudioClip>("Audio");
        }

        // Find the other dog in the scene
        Dog[] allDogs = FindObjectsOfType<Dog>();
        foreach (Dog dog in allDogs)
        {
            if (dog != this)
            {
                otherDog = dog;
                break;
            }
        }
    }

    private void Update()
    {
        // Idle tail wag
        if (_tail != null)
        {
            float angle = Mathf.Sin(Time.time * WagSpeed + _wagOffset) * WagAmplitude;
            _tail.localRotation = _tailRestRot * Quaternion.Euler(0f, angle, 0f);
        }

        if (otherDog != null)
        {
            float distanceToOtherDog = Vector3.Distance(transform.position, otherDog.transform.position);
            
            // Bark if other dog is in range and cooldown has passed
            if (distanceToOtherDog < detectionRange && Time.time - lastBarkTime > barkCooldown)
            {
                Bark();
            }
        }
    }

    private void Bark()
    {
        lastBarkTime = Time.time;
        Debug.Log(gameObject.name + " says: WOOF!");
        
        // Play bark sound if available
        if (barkSounds.Length > 0)
        {
            audioSource.PlayOneShot(barkSounds[Random.Range(0, barkSounds.Length)]);
        }
        else
        {
            // Generate a simple bark sound
            StartCoroutine(GenerateBarkSound());
        }
        
        // More obvious visual feedback
        StartCoroutine(BarkAnimation());
    }

    private IEnumerator GenerateBarkSound()
    {
        // Create a simple bark-like sound using AudioSource
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.volume = 0.5f;
        
        // Play a short beep sound
        float frequency = 220f; // A3 note
        int sampleRate = 44100;
        int sampleCount = sampleRate / 4; // 0.25 second
        
        AudioClip barkClip = AudioClip.Create("Bark", sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];
        
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * Mathf.Exp(-t * 8f); // Decaying sine wave
        }
        
        barkClip.SetData(samples, 0);
        audioSource.clip = barkClip;
        audioSource.Play();
        
        yield return null;
    }

    private IEnumerator BarkAnimation()
    {
        // Make the dog "jump" and scale up when barking
        Vector3 originalScale = transform.localScale;
        Vector3 originalPosition = transform.position;
        
        // Scale up and move up slightly
        transform.localScale = originalScale * 1.3f;
        transform.position = originalPosition + Vector3.up * 0.2f;
        
        yield return new WaitForSeconds(0.1f);
        
        // Return to normal
        transform.localScale = originalScale;
        transform.position = originalPosition;
    }
}