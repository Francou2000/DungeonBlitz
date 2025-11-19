using Photon.Pun;
using UnityEditor.Animations;
using UnityEngine;

public class ShopPlayer : MonoBehaviourPunCallbacks
{
    [SerializeField] AnimatorController[] animators;
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
        HeroesList heroe_idx = unitLoaderController.heroes[PhotonNetwork.LocalPlayer.ActorNumber].my_data.heroe_id;
        my_sprint_renderer.sprite = startSprite[(int)heroe_idx + 1];
        my_animator.runtimeAnimatorController = animators[(int)heroe_idx + 1];
    }

    
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            my_animator.SetBool("Walking", true);
            is_walking = true;
            pos_to_move = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            dir_to_move = (pos_to_move - (Vector2)transform.position).normalized;
            transform.localScale = dir_to_move.x > 0 ? Vector3.one : new Vector3(-1, 1, 1);
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
