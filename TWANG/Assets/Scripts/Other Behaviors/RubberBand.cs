using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RubberBand : MonoBehaviour
{
    private bool dead = false;
    [SerializeField] private Material whiteMat;
    private SpriteRenderer sr;
    private float lifetime = 1f;
    private int bounces = 0;
    private bool spiral = false;
    private bool facingDir = false;

    //this code sucks, cleanup later
    public float initialSpeed = 5f;
    public float spiralGrowthRate = 0.5f;
    public float rotationSpeed = 100f;
    public float attackPower = 1f;
    private float currentAngle = 0f;
    private float currentRadius = 0f;

    private int attackState;

    private Rigidbody2D rb;
    void Start()
    {
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("PlayerEffect"));
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("LevelBoundary"), LayerMask.NameToLayer("PlayerEffect"));

        sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        StartCoroutine(nameof(TimedDestroy));

        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (dead) return;

        if (collision.gameObject.layer == 6)
        {
            Physics2D.IgnoreCollision(collision.collider, collision.otherCollider);
        }
        //ass code fix later
        if (collision.gameObject.tag == "Wall")
        {
            SoundManager.Instance.PlaySoundEffect("band_hit");
            CameraManager.Instance.ScreenShake();
            if (bounces <= 0)
            {
                dead = true;
                StartCoroutine(nameof(DestroyProjectileCoroutine));
            }
            else
            {
                bounces--;
                StartCoroutine(nameof(FlashWhite));
            }
        }
        // idk enemy tag
        if (collision.gameObject.tag == "Enemy")
        {
            collision.transform.GetComponent<Entity>().stats.TakeDamage((int)attackPower);
            if (!GameManager.Instance.upgradeList.ContainsKey(UpgradeType.Ghost))
            {
                dead = true;
                StartCoroutine(nameof(DestroyProjectileCoroutine));
            }
            else { GameManager.Instance.DecPowerUp(UpgradeType.Ghost);}
        }
    }

    public void InitializeProjectile(int state)
    {
        //facingDir = facingDirection;
        attackState = state;
        switch (state)
        {
            case 1:
                bounces = 1;
                break;
            case 2:
                bounces = 2;
                break;
            case 3:
                bounces = 3;
                break;
            default: //and 0

                break;
        }
    }

    private IEnumerator FlashWhite()
    {
        var prevMat = sr.material;
        var prevColor = sr.color;
        sr.material = whiteMat;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        sr.material = prevMat;
        sr.color = prevColor;
    }

    private IEnumerator TimedDestroy()
    {
        yield return new WaitForSeconds(lifetime);
        if (!dead)
            StartCoroutine(nameof(DestroyProjectileCoroutine));
    }

    private IEnumerator DestroyProjectileCoroutine()
    {
        dead = true;

        Destroy(GetComponent<Rigidbody2D>());
        Destroy(GetComponent<Collider2D>());
        //optimize later

        sr.material = whiteMat;
        sr.color = Color.white;

        var seq = DOTween.Sequence();
        seq.AppendInterval(0.1f);
        seq.Append(transform.GetChild(0).DOScale(Vector2.zero, 0.15f));
        yield return new WaitForSeconds(0.25f);
        Destroy(gameObject);
    }
}
