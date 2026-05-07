using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopPlayer : MonoBehaviourPunCallbacks
{
    [SerializeField] RuntimeAnimatorController[] animators;
    [SerializeField] Animator my_animator;
    [SerializeField] Sprite[] startSprite;
    [SerializeField] SpriteRenderer my_sprint_renderer;
    UnitLoaderController unitLoaderController;
    PhotonView _photonView;

    Vector2 pos_to_move;
    Vector2 dir_to_move;
    public float move_speed;
    bool is_walking = false;

    void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }

    void Start()
    {
        unitLoaderController = UnitLoaderController.Instance;
        if (PhotonNetwork.IsMasterClient)
        {
            // Master is in the same scene but should not see / interact with shop avatars.
            if (my_sprint_renderer != null) my_sprint_renderer.enabled = false;
            if (my_animator != null) my_animator.enabled = false;
            foreach (var c in GetComponentsInChildren<Collider2D>(true))
                c.enabled = false;
            enabled = false;
            return;
        }

        if (_photonView == null)
        {
            ApplyHeroVisual((int)unitLoaderController.my_heroe);
            return;
        }

        if (_photonView.IsMine)
        {
            int idx = (int)unitLoaderController.my_heroe;
            ApplyHeroVisual(idx);
            _photonView.RPC(nameof(RPC_ApplyHeroVisual), RpcTarget.OthersBuffered, idx);
        }
    }

    void ApplyHeroVisual(int heroeIdx)
    {
        if (heroeIdx < 0 || heroeIdx >= startSprite.Length || heroeIdx >= animators.Length) return;
        my_sprint_renderer.sprite = startSprite[heroeIdx];
        my_animator.runtimeAnimatorController = animators[heroeIdx];
    }

    [PunRPC]
    void RPC_ApplyHeroVisual(int heroeIdx)
    {
        ApplyHeroVisual(heroeIdx);
    }

    void Update()
    {
        if (_photonView != null && !_photonView.IsMine)
            return;

        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            my_animator.SetBool("Walking", true);
            is_walking = true;
            pos_to_move = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dir_to_move = (pos_to_move - (Vector2)transform.position).normalized;
            transform.localScale = dir_to_move.x > 0 ? Vector3.one : new Vector3(-1, 1, 1);
            HeroesShopManager.instance.HideBuyUI();
        }

        if ((pos_to_move - (Vector2)transform.position).magnitude < 0.5f)
        {
            if (is_walking)
            {
                my_animator.SetBool("Walking", false);
                is_walking = false;
                var item = Physics2D.OverlapCircle(transform.position, 1);
                if (item != null)
                {
                    Debug.Log("dsgsg");
                    item.GetComponent<ItemDetector>().ShowUI();
                }
            }
            return;
        }
        transform.Translate(dir_to_move * move_speed * Time.deltaTime);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("A");
        if (collision.GetComponent<ItemDetector>() == null) return;
        Debug.Log("B");
        collision.GetComponent<ItemDetector>().ShowUI();
    }
    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ItemDetector>() == null) return;
        collision.GetComponent<ItemDetector>().HideUI();
    }
}
