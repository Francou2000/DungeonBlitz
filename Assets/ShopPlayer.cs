using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShopPlayer : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] RuntimeAnimatorController[] animators;
    [SerializeField] Animator my_animator;
    [SerializeField] Sprite[] startSprite;
    [SerializeField] SpriteRenderer my_sprint_renderer;
    UnitLoaderController unitLoaderController;

    Vector2 pos_to_move;
    Vector2 dir_to_move;
    public float move_speed;
    bool is_walking = false;
    void Start()
    {
        unitLoaderController = UnitLoaderController.Instance;
        HeroesList heroe_idx = unitLoaderController.my_heroe;
        my_sprint_renderer.sprite = startSprite[(int)heroe_idx];
        my_animator.runtimeAnimatorController = animators[(int)heroe_idx];
    }

    void Update()
    {
        if (!photonView.IsMine) return;

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
                    item.GetComponent<ItemDetector>().ShowUI();
                }
            }
            return;
        }

        transform.Translate(dir_to_move * move_speed * Time.deltaTime);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.localScale);
            stream.SendNext(is_walking);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.localScale = (Vector3)stream.ReceiveNext();
            bool walkingState = (bool)stream.ReceiveNext();
            my_animator.SetBool("Walking", walkingState);
            is_walking = walkingState;
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<ItemDetector>() == null) return;
        collision.GetComponent<ItemDetector>().ShowUI();
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ItemDetector>() == null) return;
        collision.GetComponent<ItemDetector>().HideUI();
    }
}
