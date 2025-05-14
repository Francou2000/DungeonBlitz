using UnityEngine;

public class UnitView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Unit unit;

    public GameObject outlineObject;

    public void Initialize(Unit unit)
    {
        this.unit = unit;
        outlineObject.SetActive(false);
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
    
    public void SetHighlighted(bool highlighted)
    {
        outlineObject.SetActive(highlighted);
    }  

    // Add VFX, UI feedback, etc., as needed
}
