using Photon.Pun;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Form visuals")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private RuntimeAnimatorController defaultAnimator;

    [SerializeField] private UnitJuice juice;
    private PhotonView pv;

    private Unit unit;

    public GameObject outlineObject;

    [System.Serializable]
    public class FormVisual
    {
        public string formId;                       // e.g. "Fire", "Frost", "Lightning"
        public Sprite bodySprite;                   // sprite to use in that form
        public RuntimeAnimatorController animatorController; // animator for that form
    }


    [SerializeField]
    private System.Collections.Generic.List<FormVisual> formVisuals
        = new System.Collections.Generic.List<FormVisual>();

    public void Initialize(Unit unit)
    {
        this.unit = unit;
        outlineObject.SetActive(false);
        if (!juice) juice = GetComponent<UnitJuice>();
        pv = GetComponent<PhotonView>();

        // Subscribe to form changes
        if (unit != null && unit.Model != null)
        {
            unit.Model.OnFormChanged += OnFormChanged;

            // Apply current form immediately (in case the unit spawns already attuned)
            var currentForm = unit.Model.CurrentFormId;
            ApplyFormVisual(string.IsNullOrEmpty(currentForm) ? null : currentForm);
        }
        else
        {
            // No model? Just fall back to defaults.
            ApplyFormVisual(null);
        }

        // Inicializar el parámetro Walking en false (Idle)
        SetWalking(false);
    }

    void OnDestroy()
    {
        if (unit != null && unit.Model != null)
        {
            unit.Model.OnFormChanged -= OnFormChanged;
        }
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

    // ----- Local Helpers -----

    private void PlayMoveStartLocal(Vector2 dir) 
    { 
        SetWalking(true);
        if (juice) juice.MoveStartSquash(dir); 
    }
    
    private void PlayMoveLandLocal() 
    { 
        SetWalking(false);
        if (juice) juice.MoveLandRebound(); 
    }
    
    private void SetWalking(bool walking)
    {
        if (animator != null)
        {
            animator.SetBool("Walking", walking);
        }
    }
    private void PlayAttackLocal(Vector2 facing)
    {
        if (animator) animator.Play("Attack");// if you have it
        if (juice) juice.AttackPunch(facing);
    }
    private void PlayHitLocal(Vector2 fromAtk) { if (juice) juice.HitNudge(fromAtk); }
    private void PlayMissLocal(Vector2 fromAtk) { if (juice) juice.MissDodge(fromAtk); }

    // ----- Form Visuals -----

    void OnFormChanged(string newFormId)
    {
        ApplyFormVisual(newFormId);
    }

    void ApplyFormVisual(string formId)
    {
        // TEMP: debug
        string available = "";
        for (int i = 0; i < formVisuals.Count; i++)
        {
            if (formVisuals[i] != null)
                available += formVisuals[i].formId + ", ";
        }
        Debug.Log($"[UnitView] ApplyFormVisual formId='{formId}', available=[{available}] on {name}");


        // Choose the matching entry, if any
        FormVisual match = null;

        if (!string.IsNullOrEmpty(formId))
        {
            for (int i = 0; i < formVisuals.Count; i++)
            {
                var fv = formVisuals[i];
                if (fv != null && fv.formId == formId)
                {
                    match = fv;
                    break;
                }
            }
        }

        // Decide what to apply
        Sprite targetSprite = defaultSprite;
        RuntimeAnimatorController targetAnimator = defaultAnimator;

        if (match != null)
        {
            if (match.bodySprite != null)
                targetSprite = match.bodySprite;
            if (match.animatorController != null)
                targetAnimator = match.animatorController;
        }

        // Apply to renderer + animator
        if (spriteRenderer != null && targetSprite != null)
            spriteRenderer.sprite = targetSprite;

        if (animator != null && targetAnimator != null &&
            animator.runtimeAnimatorController != targetAnimator)
        {
            animator.runtimeAnimatorController = targetAnimator;
        }
    }

    // ----- Networked Calls -----

    public void PlayMoveStartNet(Vector2 dir)
    {
        if (pv) pv.RPC(nameof(RPC_PlayMoveStart), Photon.Pun.RpcTarget.All, dir.x, dir.y);
        else RPC_PlayMoveStart(dir.x, dir.y);
    }
    public void PlayMoveLandNet()
    {
        if (pv) pv.RPC(nameof(RPC_PlayMoveLand), Photon.Pun.RpcTarget.All);
        else RPC_PlayMoveLand();
    }
    public void PlayAttackNet(Vector2 facing)
    {
        if (pv) pv.RPC(nameof(RPC_PlayAttack), Photon.Pun.RpcTarget.All, facing.x, facing.y);
        else RPC_PlayAttack(facing.x, facing.y);
    }
    public void PlayHitNet(Vector2 fromAttacker)
    {
        if (pv) pv.RPC(nameof(RPC_PlayHit), Photon.Pun.RpcTarget.All, fromAttacker.x, fromAttacker.y);
        else RPC_PlayHit(fromAttacker.x, fromAttacker.y);
    }
    public void PlayMissNet(Vector2 fromAttacker)
    {
        if (pv) pv.RPC(nameof(RPC_PlayMiss), Photon.Pun.RpcTarget.All, fromAttacker.x, fromAttacker.y);
        else RPC_PlayMiss(fromAttacker.x, fromAttacker.y);
    }

    [PunRPC] void RPC_PlayMoveStart(float x, float y) { PlayMoveStartLocal(new Vector2(x, y)); }
    [PunRPC] void RPC_PlayMoveLand() { PlayMoveLandLocal(); }
    [PunRPC] void RPC_PlayAttack(float x, float y) { PlayAttackLocal(new Vector2(x, y)); }
    [PunRPC] void RPC_PlayHit(float x, float y) { PlayHitLocal(new Vector2(x, y)); }
    [PunRPC] void RPC_PlayMiss(float x, float y) { PlayMissLocal(new Vector2(x, y)); }
}
