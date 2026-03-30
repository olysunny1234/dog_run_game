using UnityEngine;
using System.Collections;

/// <summary>
/// Drives the ithappy Dog_001 prefab as the player character.
/// WASD / Arrow Keys = move   SHIFT = run   SPACE = jump   B = bark
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DogPlayerInputController : MonoBehaviour
{
    [Header("Movement — realistic dog biomechanics")]
    public float walkSpeed    = 3.2f;   // trot (m/s)
    public float runSpeed     = 7.0f;   // gallop (m/s)
    public float acceleration = 5.0f;   // m/s²
    public float deceleration = 8.0f;
    public float turnSpeedWalk = 360f;  // deg/s at walk
    public float turnSpeedRun  = 150f;  // deg/s at run
    public float jumpForce    = 5.0f;   // ~1.27m height

    [Header("Bark")]
    public float barkCooldown  = 1.5f;
    public float ballKickForce = 3.5f;
    public AudioClip[] barkClips;

    private CharacterController _cc;
    private Animator             _animator;
    private AudioSource          _audio;

    private float _gravityVel;
    private float _currentSpeed;
    private float _lastBarkTime = -999f;
    private float _animVert;
    private float _animState;

    private void Awake()
    {
        // Must remove MovePlayerInput first (it RequireComponent-depends on CreatureMover)
        var moveInput = GetComponent("MovePlayerInput") as MonoBehaviour;
        if (moveInput != null) Destroy(moveInput);
        var mover = GetComponent("CreatureMover") as MonoBehaviour;
        if (mover != null) Destroy(mover);
    }

    private void Start()
    {
        _cc       = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.spatialBlend = 0f; // 2D: always audible regardless of camera distance
        _audio.volume       = 1f;
    }

    private void Update()
    {
        MoveAndAnimate();

        if (Input.GetKeyDown(KeyCode.B) &&
            Time.time - _lastBarkTime >= barkCooldown)
        {
            StartCoroutine(DoBark());
        }
    }

    // -------------------------------------------------------------------------
    private void MoveAndAnimate()
    {
        float v   = Input.GetAxis("Vertical");
        float h   = Input.GetAxis("Horizontal");
        bool  run = Input.GetKey(KeyCode.LeftShift);

        // Camera-relative 8-directional movement
        Camera cam = Camera.main;
        Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 camRight   = cam != null ? cam.transform.right   : Vector3.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = camForward * v + camRight * h;
        float inputMag = inputDir.magnitude;
        if (inputMag > 1f) inputDir /= inputMag;

        // Target speed with smooth acceleration
        float maxSpeed = run ? runSpeed : walkSpeed;
        float targetSpeed = Mathf.Clamp01(inputDir.magnitude) * maxSpeed;
        float rate = (targetSpeed > _currentSpeed) ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);

        // Rotate dog to face movement direction
        if (inputDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir, Vector3.up);
            float speedRatio = _currentSpeed / runSpeed;
            float effectiveTurn = Mathf.Lerp(turnSpeedWalk, turnSpeedRun, speedRatio);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, effectiveTurn * Time.deltaTime);
        }

        // Jump
        if (_cc.isGrounded && _gravityVel < 0f)
            _gravityVel = -1f;
        if (Input.GetKeyDown(KeyCode.Space) && _cc.isGrounded)
            _gravityVel = jumpForce;
        _gravityVel += Physics.gravity.y * Time.deltaTime;

        // Translate
        Vector3 mv = transform.forward * (_currentSpeed * Time.deltaTime);
        mv.y = _gravityVel * Time.deltaTime;
        _cc.Move(mv);

        // Drive Dog.controller Animator
        float animTarget = Mathf.Clamp01(_currentSpeed / walkSpeed);
        _animVert  = Mathf.MoveTowards(_animVert,  animTarget, Time.deltaTime * 5f);
        _animState = Mathf.MoveTowards(_animState, run ? 1f : 0f, Time.deltaTime * 5f);
        if (_animator != null)
        {
            _animator.SetFloat("Vert",  _animVert);
            _animator.SetFloat("State", _animState);
        }
    }

    private IEnumerator DoBark()
    {
        _lastBarkTime = Time.time;
        SpawnBarkAudio();
        Debug.Log(name + ": WOOF!");
        yield return null;
    }

    private AudioClip[] _trimmedClips;

    private void SpawnBarkAudio()
    {
        // Use real bark clips, trimmed to max 0.4s
        if (barkClips == null || barkClips.Length == 0)
            barkClips = Resources.LoadAll<AudioClip>("Audio");

        if (barkClips != null && barkClips.Length > 0)
        {
            if (_trimmedClips == null)
            {
                _trimmedClips = new AudioClip[barkClips.Length];
                for (int c = 0; c < barkClips.Length; c++)
                    _trimmedClips[c] = TrimClip(barkClips[c], 0.4f);
            }
            AudioClip clip = _trimmedClips[Random.Range(0, _trimmedClips.Length)];
            _audio.pitch = Random.Range(0.90f, 1.10f);
            _audio.PlayOneShot(clip, 1f);
            return;
        }

        // Fallback: procedural bark
        const int rate = 22050;
        int       len  = rate / 3;
        float[]   buf  = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / rate;
            float e = Mathf.Exp(-t * 8f) + Mathf.Exp(-t * 40f) * 0.5f;
            buf[i]  = (
                Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 380f * t) * 0.30f +
                Mathf.Sin(2f * Mathf.PI * 560f * t) * 0.15f +
                Mathf.Sin(2f * Mathf.PI * 750f * t) * 0.10f
            ) * e;
        }
        float peak = 0f;
        foreach (float s in buf) peak = Mathf.Max(peak, Mathf.Abs(s));
        if (peak > 0.001f)
            for (int i = 0; i < len; i++) buf[i] = buf[i] / peak * 0.9f;
        AudioClip fallback = AudioClip.Create("Bark", len, 1, rate, false);
        fallback.SetData(buf, 0);
        _audio.pitch = Random.Range(0.85f, 1.15f);
        _audio.PlayOneShot(fallback, 1f);
    }

    private static AudioClip TrimClip(AudioClip source, float maxSeconds)
    {
        if (source.length <= maxSeconds) return source;
        int samples = Mathf.FloorToInt(maxSeconds * source.frequency) * source.channels;
        float[] data = new float[samples];
        source.GetData(data, 0);
        int fadeStart = Mathf.FloorToInt(samples * 0.9f);
        for (int i = fadeStart; i < samples; i++)
            data[i] *= 1f - (float)(i - fadeStart) / (samples - fadeStart);
        AudioClip trimmed = AudioClip.Create(source.name + "_short", samples / source.channels, source.channels, source.frequency, false);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.GetComponent<BallController>() == null) return;
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;
        Vector3 dir = (hit.gameObject.transform.position - transform.position).normalized;
        dir.y = 0.25f;
        dir.Normalize();
        rb.AddForce(dir * ballKickForce, ForceMode.Impulse);
    }

    private void OnGUI()
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        s.normal.textColor = Color.white;
        GUI.Label(new Rect(12, 10, 420, 120), "WASD / Arrows  move   SHIFT  run\nSPACE  jump   B  bark\nScroll  zoom   MMB drag  orbit camera", s);
    }
}
