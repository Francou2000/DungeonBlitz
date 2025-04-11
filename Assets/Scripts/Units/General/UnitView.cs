using UnityEngine;

public class UnitView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Unit unit;

    public void Initialize(Unit unit)
    {
        this.unit = unit;
    }

    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    // Add VFX, UI feedback, etc., as needed
}
