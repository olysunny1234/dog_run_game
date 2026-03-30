using UnityEngine;
using System.Collections;

/// <summary>
/// Handles player input for the dog character.
/// WASD / Arrow Keys = move and turn
/// SPACE = jump   B = bark (opens jaw + plays sound)
/// The dog kicks the ball automatically on contact.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerDogController : MonoBehaviour
{
    [Header("Movement — realistic dog biomechanics")]
    public float walkSpeed     = 3.2f;   // trot speed (m/s)
    public float runSpeed      = 7.0f;   // gallop speed (m/s)
    public float acceleration  = 5.0f;   // m/s²
    public float deceleration  = 8.0f;   // braking is quicker
    public float rotSpeedWalk  = 360f;   // deg/s standing/walk
    public float rotSpeedRun   = 150f;   // deg/s at full run (wider arcs)
    public float jumpForce     = 5.0f;   // ~1.27 m jump height (medium dog)

    [Header("Bark")]
    public float barkCooldown  = 1.5f;
    public float ballKickForce = 3.5f;   // nose-push force (realistic)
    public AudioClip[] barkClips;

    // Components
    private CharacterController _cc;
    private AudioSource          _audio;

    // Movement state
    private float _currentSpeed;
    private float _legCycle;
    private bool  _isMoving;
    private bool  _isBarking;
    private float _lastBarkTime = -999f;
    private float _verticalVel;
    private bool  _isGrounded;
    private bool  _isRunning;

    // Cached transforms
    private Transform _frontLeftLeg;
    private Transform _frontRightLeg;
    private Transform _backLeftLeg;
    private Transform _backRightLeg;
    private Transform _tail;
    private Transform _lowerJaw;

    // Rest rotations for legs
    private Quaternion _flRest;
    private Quaternion _frRest;
    private Quaternion _blRest;
    private Quaternion _brRest;

    // Animator (for skinned models like German Shepherd)
    private Animator _animator;
    private bool _hasSpeedParam;
    private bool _hasJumpParam;
    private bool _hasTurnParam;

    private void Start()
    {
        _cc    = GetComponent<CharacterController>();
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();

        // Force 2D audio so bark is always audible regardless of camera distance
        _audio.spatialBlend = 0f;
        _audio.volume       = 1f;

        // Check for Animator (German Shepherd or other skinned model)
        _animator = GetComponent<Animator>();
        InitAnimatorParams();

        _frontLeftLeg  = transform.Find("FrontLeftLeg");
        _frontRightLeg = transform.Find("FrontRightLeg");
        _backLeftLeg   = transform.Find("BackLeftLeg");
        _backRightLeg  = transform.Find("BackRightLeg");
        _tail          = transform.Find("Tail");
        _lowerJaw      = transform.Find("LowerJaw");

        if (_frontLeftLeg)  _flRest = _frontLeftLeg.localRotation;
        if (_frontRightLeg) _frRest = _frontRightLeg.localRotation;
        if (_backLeftLeg)   _blRest = _backLeftLeg.localRotation;
        if (_backRightLeg)  _brRest = _backRightLeg.localRotation;

        // Find head/neck bones for skinned models (German Shepherd)
        FindHeadBones();
    }

    private void InitAnimatorParams()
    {
        if (_animator != null && _animator.runtimeAnimatorController != null && _animator.parameterCount > 0)
        {
            foreach (var p in _animator.parameters)
            {
                if (p.name == "Speed") _hasSpeedParam = true;
                if (p.name == "IsJumping") _hasJumpParam = true;
                if (p.name == "TurnDir") _hasTurnParam = true;
            }
            Debug.Log("Animator params: Speed=" + _hasSpeedParam + " Jump=" + _hasJumpParam + " Turn=" + _hasTurnParam);
        }
    }

    private void Update()
    {
        // Retry animator init if it wasn't ready at Start
        if (_animator != null && !_hasSpeedParam)
            InitAnimatorParams();

        HandleMovement();
        HandleInput();
        // Only use procedural animation for primitive-based dogs
        // Skinned models (German Shepherd) use Animator clips instead
        if (!_hasSpeedParam)
        {
            AnimateLegs();
            AnimateTail();
        }
    }

    // Must run after Animator to override bone rotations
    private void LateUpdate()
    {
        if (_headBonesFound)
            ApplyHeadLook();
    }

    // ---- Movement --------------------------------------------------------
    private void HandleMovement()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
        _isRunning = Input.GetKey(KeyCode.LeftShift);

        // Camera-relative 8-directional movement
        // W = forward, S = back, A = left, D = right (relative to camera)
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

        // Target speed based on input magnitude + shift
        float maxSpeed = _isRunning ? runSpeed : walkSpeed;
        float targetSpeed = Mathf.Clamp01(inputDir.magnitude) * maxSpeed;

        // Smooth acceleration / deceleration
        float rate = (targetSpeed > _currentSpeed) ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);

        // Rotate dog to face movement direction
        if (inputDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDir, Vector3.up);
            float speedRatio = _currentSpeed / runSpeed;
            float turnRate = Mathf.Lerp(rotSpeedWalk, rotSpeedRun, speedRatio);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, turnRate * Time.deltaTime);
        }

        // Gravity and jump
        _isGrounded = _cc.isGrounded;
        if (_isGrounded && _verticalVel < 0f)
            _verticalVel = -1f;

        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            _verticalVel = jumpForce;

        _verticalVel += Physics.gravity.y * Time.deltaTime;

        // Apply movement along dog's facing direction
        Vector3 move = transform.forward * (_currentSpeed * Time.deltaTime);
        move.y = _verticalVel * Time.deltaTime;
        _cc.Move(move);

        _isMoving = _currentSpeed > 0.1f;
        if (_isMoving)
            _legCycle += Time.deltaTime * _currentSpeed * 1.5f;

        // Drive animator for skinned models (German Shepherd)
        if (_hasSpeedParam)
        {
            float normalizedSpeed = _currentSpeed / runSpeed;
            _animator.SetFloat("Speed", normalizedSpeed);

            // Scale animation playback speed to match ground movement
            // Walk clip is 0.6s, Run clip is 0.4s — sync foot placement
            float animSpeed = 1f;
            if (normalizedSpeed > 0.55f)
                animSpeed = Mathf.Lerp(0.8f, 1.5f, (normalizedSpeed - 0.55f) / 0.45f);
            else if (normalizedSpeed > 0.1f)
                animSpeed = Mathf.Lerp(0.6f, 1.2f, (normalizedSpeed - 0.1f) / 0.45f);
            _animator.speed = _isGrounded ? animSpeed : 1f;
        }
        if (_hasJumpParam)
            _animator.SetBool("IsJumping", !_isGrounded);
        if (_hasTurnParam)
        {
            float turnInput = Input.GetAxis("Horizontal");
            _animator.SetFloat("TurnDir", Mathf.Lerp(
                _animator.GetFloat("TurnDir"), turnInput, Time.deltaTime * 5f));
        }

        // Procedural jump body tilt (works on top of skeletal animation)
        if (_hasSpeedParam)
            ApplyJumpTilt();
    }

    // ---- Jump Body Tilt ---------------------------------------------------
    private float _jumpTiltAngle;
    private Transform _modelRoot;
    private Quaternion _modelBaseRot;
    private bool _modelRootInit;

    private void ApplyJumpTilt()
    {
        // Find the mesh root child (first child with a SkinnedMeshRenderer)
        if (_modelRoot == null && transform.childCount > 0)
        {
            // Look for the skeleton root, not the mesh
            foreach (Transform child in transform)
            {
                if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                {
                    _modelRoot = child;
                    break;
                }
            }
            if (_modelRoot == null) _modelRoot = transform.GetChild(0);
        }
        if (_modelRoot == null) return;

        if (!_modelRootInit)
        {
            _modelBaseRot = _modelRoot.localRotation;
            _modelRootInit = true;
        }

        float targetTilt = 0f;
        if (!_isGrounded)
        {
            // Subtle tilt: -8 degrees on ascent, +5 on descent
            if (_verticalVel > 0.5f)
                targetTilt = -8f;    // slight nose-down on launch
            else if (_verticalVel < -0.5f)
                targetTilt = 5f;     // slight nose-up on descent
            else
                targetTilt = -3f;    // near-level at apex
        }

        _jumpTiltAngle = Mathf.Lerp(_jumpTiltAngle, targetTilt, Time.deltaTime * 10f);
        _modelRoot.localRotation = _modelBaseRot * Quaternion.Euler(_jumpTiltAngle, 0f, 0f);
    }

    // ---- Head Look ---------------------------------------------------------
    private Transform _neckBone; // DEF-spine.010
    private Transform _headBone; // DEF-spine.011
    private bool _headBonesFound;
    private float _headYaw;

    private void FindHeadBones()
    {
        _neckBone = FindDeepChild(transform, "DEF-spine.010");
        _headBone = FindDeepChild(transform, "DEF-spine.011");
        _headBonesFound = (_neckBone != null && _headBone != null);
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void ApplyHeadLook()
    {
        if (!_headBonesFound) return;

        // Head turn from horizontal input only (walk direction handled by blend tree)
        float hInput = Input.GetAxis("Horizontal");
        _headYaw = Mathf.Lerp(_headYaw, hInput * 20f, Time.deltaTime * 6f);

        // Additive head/neck turn for look direction
        _neckBone.localRotation *= Quaternion.Euler(_headYaw * 0.4f, 0f, 0f);
        _headBone.localRotation *= Quaternion.Euler(_headYaw * 0.6f, 0f, 0f);
    }

    // ---- Input -----------------------------------------------------------
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.B) && !_isBarking &&
            (Time.time - _lastBarkTime) >= barkCooldown)
        {
            StartCoroutine(BarkCoroutine());
        }
    }

    // ---- Bark ------------------------------------------------------------
    private IEnumerator BarkCoroutine()
    {
        _isBarking   = true;
        _lastBarkTime = Time.time;

        PlayBarkSound();
        Debug.Log(name + ": WOOF! WOOF!");

        if (_lowerJaw != null)
        {
            Quaternion closed = _lowerJaw.localRotation;
            Quaternion open   = closed * Quaternion.Euler(38f, 0f, 0f);

            yield return LerpJaw(closed, open, 12f);
            yield return LerpJaw(open, closed, 10f);
            yield return new WaitForSeconds(0.08f);
            yield return LerpJaw(closed, open, 14f);
            yield return LerpJaw(open, closed, 12f);
        }
        else
        {
            yield return new WaitForSeconds(0.35f);
        }

        _isBarking = false;
    }

    private IEnumerator LerpJaw(Quaternion from, Quaternion to, float speed)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            if (_lowerJaw != null)
                _lowerJaw.localRotation = Quaternion.Slerp(from, to, Mathf.Clamp01(t));
            yield return null;
        }
    }

    private AudioClip[] _trimmedClips;

    private void PlayBarkSound()
    {
        // Use real bark clips if available, trimmed to max 0.4s
        if (barkClips == null || barkClips.Length == 0)
            barkClips = Resources.LoadAll<AudioClip>("Audio");

        if (barkClips != null && barkClips.Length > 0)
        {
            // Trim clips once on first use
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
        const int sampleRate = 22050;
        int length = sampleRate / 3;
        float[] data = new float[length];
        for (int i = 0; i < length; i++)
        {
            float t   = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 8f) + Mathf.Exp(-t * 40f) * 0.5f;
            data[i] = (
                Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 380f * t) * 0.30f +
                Mathf.Sin(2f * Mathf.PI * 560f * t) * 0.15f +
                Mathf.Sin(2f * Mathf.PI * 750f * t) * 0.10f
            ) * env;
        }
        float peak = 0f;
        foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
        if (peak > 0.001f)
            for (int i = 0; i < length; i++) data[i] = data[i] / peak * 0.9f;
        AudioClip fallback = AudioClip.Create("Bark", length, 1, sampleRate, false);
        fallback.SetData(data, 0);
        _audio.pitch = Random.Range(0.85f, 1.15f);
        _audio.PlayOneShot(fallback, 1f);
    }

    private static AudioClip TrimClip(AudioClip source, float maxSeconds)
    {
        if (source.length <= maxSeconds) return source;
        int samples = Mathf.FloorToInt(maxSeconds * source.frequency) * source.channels;
        float[] data = new float[samples];
        source.GetData(data, 0);
        // Apply fade-out on last 10% to avoid click
        int fadeStart = Mathf.FloorToInt(samples * 0.9f);
        for (int i = fadeStart; i < samples; i++)
            data[i] *= 1f - (float)(i - fadeStart) / (samples - fadeStart);
        AudioClip trimmed = AudioClip.Create(source.name + "_short", samples / source.channels, source.channels, source.frequency, false);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    // ---- Leg Animation ---------------------------------------------------
    private void AnimateLegs()
    {
        // Amplitude and phase depend on speed (walk=small, gallop=large)
        float speedRatio = _currentSpeed / runSpeed;
        float amp = _isMoving ? Mathf.Lerp(20f, 45f, speedRatio) : 0f;

        // Trot gait: diagonal pairs move together (LF+RH, RF+LH)
        float fl =  Mathf.Sin(_legCycle) * amp;
        float fr = -Mathf.Sin(_legCycle) * amp;
        float bl = -Mathf.Sin(_legCycle) * amp;  // diagonal with front-right
        float br =  Mathf.Sin(_legCycle) * amp;  // diagonal with front-left

        // At gallop speed, add slight body bounce (vertical bob)
        if (_isMoving && speedRatio > 0.6f)
        {
            float bounce = Mathf.Abs(Mathf.Sin(_legCycle * 2f)) * speedRatio * 0.04f;
            // Small up/down on body parts for gallop feel
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                transform.localPosition.y,
                transform.localPosition.z);
        }

        if (_frontLeftLeg)  _frontLeftLeg.localRotation  = _flRest * Quaternion.Euler(fl, 0f, 0f);
        if (_frontRightLeg) _frontRightLeg.localRotation = _frRest * Quaternion.Euler(fr, 0f, 0f);
        if (_backLeftLeg)   _backLeftLeg.localRotation   = _blRest * Quaternion.Euler(bl, 0f, 0f);
        if (_backRightLeg)  _backRightLeg.localRotation  = _brRest * Quaternion.Euler(br, 0f, 0f);
    }

    // ---- Tail Wag --------------------------------------------------------
    private void AnimateTail()
    {
        if (_tail == null) return;
        // Wag faster when moving (excited), slower when idle
        float wagFreq = _isMoving ? 5.0f : 3.0f;
        float wagAmp  = _isMoving ? 40f : 25f;
        float wag = Mathf.Sin(Time.time * wagFreq * Mathf.PI * 2f) * wagAmp;
        _tail.localRotation = Quaternion.Euler(-52f, wag, 0f);
    }

    // ---- Ball Kick (CharacterController collision) -----------------------
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only push objects with a BallController
        if (hit.gameObject.GetComponent<BallController>() == null) return;

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        Vector3 kickDir = (hit.gameObject.transform.position - transform.position).normalized;
        kickDir.y = 0.25f;
        kickDir.Normalize();
        rb.AddForce(kickDir * ballKickForce, ForceMode.Impulse);
    }

    // ---- HUD -------------------------------------------------------------
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 16;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(12, 10, 400, 120),
            "WASD  move   SHIFT  run   SPACE  jump\nB  bark   Scroll  zoom   MMB  orbit camera", style);
    }
}
