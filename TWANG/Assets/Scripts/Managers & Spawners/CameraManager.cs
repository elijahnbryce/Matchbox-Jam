using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private Transform playerTransform;
    [SerializeField] private Vector2 maxPos;
    private Vector2 minPos;
    public static CameraManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    void Start()
    {
        playerTransform = PlayerMovement.Instance.transform;
        minPos = -1 * maxPos;

        PlayerAttack.OnAttackAim += AttackStart;
        PlayerAttack.OnAttackHalt += AttackEnd;
    }

    void Update()
    {
        var newPos = Vector2.Lerp(transform.localPosition, playerTransform.position, 5f * Time.deltaTime);
        newPos.x = Mathf.Clamp(newPos.x, minPos.x, maxPos.x);
        newPos.y = Mathf.Clamp(newPos.y, minPos.y, maxPos.y);
        //ass code fix later
        transform.localPosition = new Vector3(newPos.x, newPos.y, -10);
    }

    private void AttackStart()
    {
        //var seq = DOTween.Sequence();
        //seq.AppendInterval(0.5f);
        //seq.Append(transform.parent.DOShakePosition(100, 0.05f, 5));
        //transform.parent.DOShakePosition(100, 0.025f, 5);
    }

    private void AttackEnd()
    {
        //transform.parent.DOKill();
        //transform.parent.localPosition = Vector3.zero;
        ScreenShake(0.5f);
    }

    public void ScreenShake() => ScreenShake(0.25f);
    public void ScreenShake(float amount)
    {
        //cleanup later
        transform.parent.DOShakePosition(0.15f, amount).OnComplete(() => transform.parent.localPosition = Vector3.zero);
    }

}
