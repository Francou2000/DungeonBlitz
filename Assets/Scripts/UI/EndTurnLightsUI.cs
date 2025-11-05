using UnityEngine;
using UnityEngine.UI;

public class EndTurnLightsUI : MonoBehaviour
{
    [Header("Lights (left→right)")]
    [SerializeField] private Image light1;
    [SerializeField] private Image light2;
    [SerializeField] private Image light3;
    [SerializeField] private Image light4;

    [Header("Sprites")]
    [SerializeField] private Sprite playerNotReady; // red dot sprite
    [SerializeField] private Sprite playerReady;    // green dot sprite

    [Header("Behavior")]
    [Tooltip("If true, while it's the Monster/DM turn all lights show NOT READY.")]
    [SerializeField] private bool dmShowsAllRed = true;

    // cached snapshot
    private int[] lastActors = System.Array.Empty<int>();
    private bool[] lastStates = System.Array.Empty<bool>();

    private void OnEnable()
    {
        TurnManager.OnHeroReadySnapshot += OnSnapshot;
        TurnManager.OnTurnBegan += OnTurnBegan;
        Paint(); // draw whatever we have right now
    }

    private void OnDisable()
    {
        TurnManager.OnHeroReadySnapshot -= OnSnapshot;
        TurnManager.OnTurnBegan -= OnTurnBegan;
    }

    private void OnSnapshot(int[] actors, bool[] states)
    {
        lastActors = actors ?? System.Array.Empty<int>();
        lastStates = states ?? System.Array.Empty<bool>();
        Paint();
    }

    private void OnTurnBegan(UnitFaction side)
    {
        // On Heroes' turn: repaint from fresh snapshot (usually all false).
        // On DM turn: keep all red if requested.
        Paint();
    }

    private void Paint()
    {
        var tm = TurnManager.Instance;
        bool monsterTurn = tm && tm.currentTurn == UnitFaction.Monster;

        // decide per-light sprite
        var imgs = new[] { light1, light2, light3, light4 };
        for (int i = 0; i < imgs.Length; i++)
        {
            if (!imgs[i]) continue;

            bool showReady = (i < lastStates.Length) ? lastStates[i] : false;

            // If DM turn and you want them all red, override
            if (monsterTurn && dmShowsAllRed) showReady = false;

            imgs[i].sprite = showReady ? playerReady : playerNotReady;
            imgs[i].type = Image.Type.Simple;
            imgs[i].preserveAspect = true;
            imgs[i].raycastTarget = false; // keep clicks on the button clean
            imgs[i].enabled = true;
        }
    }
}
