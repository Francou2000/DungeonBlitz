using Photon.Pun;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private UnitJuice juice;
    private Photon.Pun.PhotonView pv;

    private Unit unit;

    public GameObject outlineObject;

    public void Initialize(Unit unit)
    {
        this.unit = unit;
        outlineObject.SetActive(false);
        if (!juice) juice = GetComponent<UnitJuice>();
        pv = GetComponent<Photon.Pun.PhotonView>();
        
        // Inicializar el parámetro Walking en false (Idle)
        SetWalking(false);
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
