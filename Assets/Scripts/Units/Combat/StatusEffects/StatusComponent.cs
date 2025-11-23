using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

/// Master is authoritative for apply/tick; clients mirror via RPC for UI only.
public class StatusComponent : MonoBehaviourPun
{
    [SerializeField] private UnitController controller;  // assign in inspector or Find in Awake
    [SerializeField] private UnitModel model;

    // Active effects (authoritative on master)
    private readonly List<StatusEffect> _active = new();

    // Cache for this turn
    private int _apDeltaThisTurn;       // Haste/Shock
    private float _healingMultThisTurn = 1f;
    private bool _movedThisTurn;

    public event System.Action<StatusEffect> OnEffectApplied;
    public event System.Action<StatusEffect> OnEffectRemoved;

    public IEnumerable<StatusEffect> ActiveEffects => _active;

    Unit _ownerUnit;

    [SerializeField] private bool disableStatuses = true;
    private bool Disabled => disableStatuses;

    void Awake()
    {
        if (!controller) controller = GetComponent<UnitController>();
        if (!model) model = GetComponent<UnitModel>();
        _ownerUnit = GetComponent<Unit>();
    }

    string OwnerName => _ownerUnit ? _ownerUnit.name : gameObject.name;

    void Log(string msg)
    {
        Debug.Log($"[STATUS] [{OwnerName}] {msg}");
    }

    // --------- Public API (call on MASTER) ---------
    public void Apply(StatusEffect e)
    {
        if (Disabled) return;
        if (!PhotonNetwork.IsMasterClient || e == null) return;

        // Non-stack: refresh if same type & same targetLock (for Taunt), else add
        if (e.type is StatusType.Bleed or StatusType.Root or StatusType.Haste or StatusType.Shock
            or StatusType.Burn or StatusType.Freeze or StatusType.Taunt or StatusType.Barrier
            or StatusType.Incandescent or StatusType.Enraged)
        {
            var same = _active.FirstOrDefault(x => x.type == e.type && (e.type != StatusType.Taunt || x.targetLockViewId == e.targetLockViewId));
            if (same != null)
            {
                same.remainingTurns = e.remainingTurns;
                same.aux = e.aux;
                same.barrierHP = e.barrierHP;
                same.sourceViewId = e.sourceViewId;
                same.targetLockViewId = e.targetLockViewId;
                Mirror(); return;
            }
        }

        Log($"APPLY {e.type} " +
                $"amount={e.amount} " +
                $"remainingTurns={e.remainingTurns} aux={e.aux} " +
                $"barrierHP={e.barrierHP} stat={e.stat} isDebuff={e.isDebuff} " +
                $"apDelta={e.nextTurnActionsDelta} healMult={e.healMultiplier} " +
                $"bleedOnMove={e.bleedDamageOnMove} targetLock={e.targetLockViewId} " +
                $"sourceViewId={e.sourceViewId}");

        Mirror();
    }

    public void Remove(StatusType t)
    {
        if (Disabled) return;
        if (!PhotonNetwork.IsMasterClient) return;

        // walk backwards so we can RemoveAt safely
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i].type == t)
            {
                var removed = _active[i];          // keep the instance
                _active.RemoveAt(i);

                Log($"REMOVE {removed.type} (explicit remove) remainingTurns={removed.remainingTurns} aux={removed.aux}");
                OnEffectRemoved?.Invoke(removed);
                OnEffectRemoved?.Invoke(removed);  // notify with the effect, not the enum
            }
        }

        Mirror();
    }

    public bool Has(StatusType t) => _active.Any(x => x.type == t);

    public bool IsRooted() => _active.Any(x => x.root);
    public bool IsIncandescent() => _active.Any(x => x.incandescent);

    public int GetAffinityBuff() => _active.Where(x => x.stat == Stat.Affinity && !x.isDebuff).Sum(x => x.amount);
    public int GetStatDelta(Stat s) => _active.Where(x => x.stat == s).Sum(x => x.isDebuff ? -x.amount : x.amount);

    public int GetBarrierPool() => _active.Sum(x => x.barrierHP);

    public int GetAPDeltaForThisTurn()
    {
        Log($"AP DELTA this turn queried = {_apDeltaThisTurn}");
        return _apDeltaThisTurn;
    }
    public float GetHealingMultiplierThisTurn() => _healingMultThisTurn;

    public bool IsTauntedTo(int viewId) => _active.Any(x => x.type == StatusType.Taunt && x.targetLockViewId == viewId);

    // --------- Hooks wired from TurnManager / Movement / Resolve ---------

    // Call when THIS unit’s turn begins (MASTER).
    public void OnTurnBegan()
    {
        if (Disabled) return;
        if (!PhotonNetwork.IsMasterClient) return;

        Log("TURN BEGAN");

        _apDeltaThisTurn = 0;
        _healingMultThisTurn = 1f;

        // Start-of-turn ticks/effects
        foreach (var e in _active.ToList())
        {
            Log($"  Begin -> {e.type} rem={e.remainingTurns} aux={e.aux} barrierHP={e.barrierHP}");

            // Burn ticks at start of victim’s next two turns (aux = ticks left)
            if (e.type == StatusType.Burn && e.aux > 0)
            {
                int tick = ComputeBurnTickFromSource(e.sourceViewId);
                Log($"    Burn tick from viewId={e.sourceViewId} damage={tick} aux(before)={e.aux}");
                AbilityResolver.Instance.ResolveDamageServerOnly(controller, tick, DamageType.Fire, e.sourceViewId, -1);
                e.aux--;
                Log($"    Burn aux(after)={e.aux}");
            }

            // Action/AP one-turn changes (Haste/Shock/Enraged)
            if (e.nextTurnActionsDelta != 0)
            {
                _apDeltaThisTurn += e.nextTurnActionsDelta;
                Log($"    AP delta contribution from {e.type} = {e.nextTurnActionsDelta}");
            }

            // Healing multiplier (poison-like or regen-like)
            if (!Mathf.Approximately(e.healMultiplier, 0f) && !Mathf.Approximately(e.healMultiplier, 1f))
            {
                _healingMultThisTurn *= Mathf.Max(0f, e.healMultiplier);
                Log($"    HealMult contribution from {e.type} = {e.healMultiplier} -> turn mult now = {_healingMultThisTurn}");
            }
        }

        Log($"  AP DELTA total this turn = {_apDeltaThisTurn}, HealMult={_healingMultThisTurn}");
        Mirror();
    }

    // Call when THIS unit’s turn ends (MASTER).
    public void OnTurnEnded()
    {
        if (Disabled) return;
        if (!PhotonNetwork.IsMasterClient) return;

        Log("TURN ENDED (before decay)");

        foreach (var e in _active)
        {
            Log($"  End(before) -> {e.type} rem={e.remainingTurns} aux={e.aux} barrierHP={e.barrierHP}");
        }

        // Bleed ends after a full turn without moving
        var bleed = _active.FirstOrDefault(x => x.type == StatusType.Bleed);
        if (bleed != null && !_movedThisTurn)
        {
            Log("  Bleed removed because unit did NOT move this turn");
            _active.Remove(bleed);
            OnEffectRemoved?.Invoke(bleed);
        }

        // Shock/Frozen expire after the turn
        int removedShock = _active.RemoveAll(x => x.type == StatusType.Shock || x.type == StatusType.Freeze);
        if (removedShock > 0)
        {
            Log($"  Removed {removedShock} Shock/Freeze effects at end of turn");
        }

        // Decrement general durations (not Burn’s aux; Barrier stays until HP is 0 or duration ends)
        foreach (var e in _active.ToList())
        {
            if (e.type is StatusType.Burn) continue; // handled by aux
            if (e.remainingTurns > 0)
            {
                e.remainingTurns--;
                Log($"  Decay {e.type} -> remainingTurns now {e.remainingTurns}");

                if (e.remainingTurns <= 0)
                {
                    Log($"  Duration over -> removing {e.type}");
                    _active.Remove(e);
                    OnEffectRemoved?.Invoke(e);
                }
            }
        }

        _movedThisTurn = false;

        Log("TURN ENDED (after decay)");
        foreach (var e in _active)
        {
            Log($"  End(after) -> {e.type} rem={e.remainingTurns} aux={e.aux} barrierHP={e.barrierHP}");
        }

        Mirror();
    }

    // Call when THIS unit successfully finishes a move (MASTER).
    public void OnMoved()
    {
        if (Disabled) return;
        if (!PhotonNetwork.IsMasterClient) return;
        _movedThisTurn = true;
        Log("MOVED this turn (flag _movedThisTurn = true)");

        var bleed = _active.FirstOrDefault(x => x.type == StatusType.Bleed && x.bleedDamageOnMove > 0);
        if (bleed != null)
        {
            Log($"  Bleed tick on move: damage={bleed.bleedDamageOnMove}");
            AbilityResolver.Instance.ResolveDamageServerOnly(controller, bleed.bleedDamageOnMove, DamageType.Physical);
        }
    }

    // Barrier consumption. Returns remaining incoming after barrier.
    public int AbsorbWithBarrier(int incoming, DamageType type)
    {
        if (incoming <= 0) return 0;
        if (!PhotonNetwork.IsMasterClient) return incoming;
        Debug.Log($"[Barrier][Absorb] {controller.name} incoming={incoming} pools={_active.Count(x => x.type == StatusType.Barrier)}");

        foreach (var e in _active)
        {
            if (e.type != StatusType.Barrier || e.barrierHP <= 0) continue;
            int used = Mathf.Min(incoming, e.barrierHP);
            e.barrierHP -= used;
            incoming -= used;
            Debug.Log($"[Barrier][Use] used={used} leftPool={e.barrierHP} incomingLeft={incoming}");
            if (incoming <= 0) break;
        }

        // Remove exhausted barrier effects
        _active.RemoveAll(x => x.type == StatusType.Barrier && x.barrierHP <= 0);
        Debug.Log($"[Barrier][After] {controller.name} remaining={incoming}");
        Mirror();
        return incoming;
    }

    // ---------- Private helpers ----------

    int ComputeBurnTickFromSource(int sourceViewId)
    {
        var srcPV = PhotonView.Find(sourceViewId);
        if (!srcPV) return 1;
        var src = srcPV.GetComponent<UnitController>()?.model;
        if (src == null) return 1;
        // spec: ½ of attacker’s STR or MAG (choose higher if you don’t store which)
        int str = src.Strength;
        int mag = src.MagicPower;
        return Mathf.Max(1, Mathf.FloorToInt(0.5f * Mathf.Max(str, mag)));
    }

    // --------- RPC mirroring (UI only on clients) ---------
    void Mirror()
    {
        if (Disabled) return;
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_Mirror), RpcTarget.Others, Serialize());
    }

    byte[] Serialize()
    {
        // Compact payload (type, name, remaining, aux, barrierHP, stat, amount, flags).
        var dto = _active.Select(e => new EffectDTO(e)).ToList();
        return System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new EffectList { list = dto }));
    }

    [PunRPC]
    void RPC_Mirror(byte[] data)
    {
        if (Disabled) return;
        if (PhotonNetwork.IsMasterClient) return; // master already knows

        // keep old list to raise removals
        var old = new List<StatusEffect>(_active);

        var json = System.Text.Encoding.UTF8.GetString(data);
        var parsed = JsonUtility.FromJson<EffectList>(json);

        _active.Clear();

        // notify removals
        if (old != null)
            foreach (var was in old)
                OnEffectRemoved?.Invoke(was);

        // fill new list + notify applies
        if (parsed?.list != null)
        {
            foreach (var dto in parsed.list)
            {
                var e = dto.ToRuntime();
                _active.Add(e);
                OnEffectApplied?.Invoke(e);    // now UI hears about it on clients
            }
        }
    }

    [System.Serializable] class EffectList { public List<EffectDTO> list; }
    [System.Serializable]
    class EffectDTO
    {
        public string n; public int t; public int r; public int a; public int b; public int s; public int am;
        public bool d; public int ap; public int ms; public float hm; public int bl; public int tl; public bool ic; public int sv;

        public EffectDTO() { }
        public EffectDTO(StatusEffect e)
        {
            n = e.name; t = (int)e.type; r = e.remainingTurns; a = e.aux; b = e.barrierHP; s = (int)e.stat; am = e.amount; d = e.isDebuff;
            ap = e.nextTurnActionsDelta; ms = e.moveSpeedDeltaPct; hm = e.healMultiplier; bl = e.bleedDamageOnMove; tl = e.targetLockViewId; ic = e.incandescent; sv = e.sourceViewId;
        }
        public StatusEffect ToRuntime() => new StatusEffect
        {
            name = n,
            type = (StatusType)t,
            remainingTurns = r,
            aux = a,
            barrierHP = b,
            stat = (Stat)s,
            amount = am,
            isDebuff = d,
            nextTurnActionsDelta = ap,
            moveSpeedDeltaPct = ms,
            healMultiplier = hm,
            bleedDamageOnMove = bl,
            targetLockViewId = tl,
            incandescent = ic,
            sourceViewId = sv
        };
    }
}
