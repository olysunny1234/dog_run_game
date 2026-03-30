using UnityEngine;

/// <summary>
/// Marks this GameObject as the ball that the dog can kick.
/// Kicked via PlayerDogController.OnControllerColliderHit.
/// </summary>
public class BallController : MonoBehaviour
{
    private void Start()
    {
        // Make sure shadows are cast
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }
}
