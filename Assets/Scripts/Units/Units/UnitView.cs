using UnityEngine;
using UnityEngine.Rendering;

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

    public void PlayOneShotAnimation(AnimationName animation_name)
    {
        if (animator == null) return;
        string trigger_name = "None";
        switch (animation_name)
        {
            case AnimationName.Attack:
                trigger_name = "atk_trg";
                break;
            case AnimationName.RangeAttack:
                trigger_name = "ratk_trg";
                break;
            case AnimationName.Evade:
                trigger_name = "evd_trg";
                break;
            case AnimationName.Hit:
                trigger_name = "hit_trg";
                break;
        }
        if (trigger_name == "None") return;

        animator.SetTrigger(trigger_name);
    }

    public void SetLoopAnimation(AnimationName animation_name)
    {
        if (animator == null) return;
        if (animation_name == AnimationName.Idle)
        {
            animator.SetBool("is_walking", false);
        }
        else if (animation_name == AnimationName.Walk)
        {
            animator.SetBool("is_walking", true);
        }
    }

    public void DeadAnimation()
    {
        if (animator == null) return;
        animator.SetBool("is_dead", false);
    }
}

public enum AnimationName
{
    Idle,
    Walk,
    Attack,
    RangeAttack,
    Evade,
    Hit,
    Dead
}

/*
 * 
 * The Animator should have each Animation Clip
 * Only Idle and Walk should be looped
 * A bool "is_walking" or "is_idle" should decide the animation to use
 * Dead Animation should use a bool too ("is_dead") 
 * For the other Animations we should use trigers
 *      - atk_trg
 *      - ratk_trg
 *      - evd_trg
 *      - hit_trg
 *      - etc...
 *
 */