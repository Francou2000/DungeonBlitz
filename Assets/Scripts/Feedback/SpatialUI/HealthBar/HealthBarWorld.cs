using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarWorld : MonoBehaviour
{
    [Header("Binding")]
    public Unit targetUnit;                 // assign via Bind() or Inspector
    public Image fill;                      // foreground fill
    public Image back;                      // optional background image

    [Header("Positioning")]
    public Vector3 worldOffset = new Vector3(0f, 1.6f, 0f);  // height above head
    public bool useRendererBounds = true;   // place above sprite bounds if available
    public float extraHeight = 0.2f;        // extra above bounds

    [Header("Visibility")]
    public bool hideWhenFull = false;
    public bool hideWhenDead = true;

    [Header("Team Colors")]
    public Color controlledColor = Color.blue;
    public Color allyColor = Color.yellow;
    public Color enemyColor = Color.red;

    Transform _follow;
    Renderer _rend;
    int _lastHP = -1, _lastMax = -1;

    public void Bind(Unit unit)
    {
        targetUnit = unit;
        if (!targetUnit)
        {
            gameObject.SetActive(false);
            return;
        }

        if (targetUnit.Model != null)
            targetUnit.Model.OnHealthChanged += OnHealthChanged;

        // Follow the visual if possible, otherwise the unit root
        _follow = (targetUnit.View != null) ? targetUnit.View.transform : targetUnit.transform;
        _rend = _follow.GetComponentInChildren<Renderer>();

        ForceRefresh();
    }

    void OnHealthChanged(int current, int max) => UpdateFill(current, max);

    void LateUpdate()
    {
        if (!targetUnit || _follow == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Keep amount + color in sync every frame (even if HP didn't change)
        if (targetUnit.Model != null)
        {
            UpdateFill(targetUnit.Model.CurrentHP, targetUnit.Model.MaxHP);
        }

        // Position above the unit
        Vector3 anchor = _follow.position;
        if (useRendererBounds && _rend != null)
        {
            var b = _rend.bounds;
            anchor = new Vector3(b.center.x, b.max.y + extraHeight, 0f);
        }
        else
        {
            anchor += worldOffset;
            anchor.z = 0f;
        }

        transform.position = anchor;
    }

    void UpdateFill(int hp, int max)
    {
        _lastHP = hp;
        _lastMax = Mathf.Max(1, max);

        if (!fill) return;

        float pct = Mathf.Clamp01((float)hp / _lastMax);
        fill.fillAmount = pct;

        // Team / control color
        Color teamCol = GetTeamColor();
        fill.color = teamCol;

        if (back)
        {
            var backCol = back.color;
            back.color = new Color(teamCol.r, teamCol.g, teamCol.b, backCol.a);
        }

        // Visibility rules
        if (hideWhenDead && hp <= 0)
        {
            SetVisible(false);
            return;
        }

        if (hideWhenFull)
        {
            SetVisible(pct < 0.999f);
        }
        else
        {
            SetVisible(true);
        }
    }

    void ForceRefresh()
    {
        if (targetUnit != null && targetUnit.Model != null)
        {
            int hp = targetUnit.Model.CurrentHP;
            int mx = targetUnit.Model.MaxHP;
            UpdateFill(hp, mx);
        }
        else
        {
            UpdateFill(0, 1);
        }
    }

    void SetVisible(bool v)
    {
        if (back) back.enabled = v;
        if (fill) fill.enabled = v;
    }

    void OnDestroy()
    {
        if (targetUnit && targetUnit.Model != null)
            targetUnit.Model.OnHealthChanged -= OnHealthChanged;
    }

    Color GetTeamColor()
    {
        if (targetUnit == null) return allyColor;

        // Local side: master client = Monster (DM), others = Hero
        UnitFaction localFaction =
            PhotonNetwork.IsMasterClient ? UnitFaction.Monster : UnitFaction.Hero;

        bool isControlled =
            UnitController.ActiveUnit != null &&
            UnitController.ActiveUnit.unit == targetUnit &&
            UnitController.ActiveUnit.photonView.IsMine;

        bool sameFaction = targetUnit.Faction == localFaction;

        if (isControlled) return controlledColor; // blue
        else if (sameFaction) return allyColor;       // yellow
        else return enemyColor;      // red
    }
}
