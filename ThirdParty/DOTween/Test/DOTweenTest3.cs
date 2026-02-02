using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DOTweenTest3 : MonoBehaviour
{
    [Header("枪械引用")]
    public Camera playerCamera;

    [Space]
    [Header("后坐力参数")]
    [Header("相机轻微向上角度与向后位置偏移量")]
    public float recoilUpwardForce = 0.5f;
    public float CameraMoveZ = 0.1f;
    [Header("摄像机执行旋转的时间")]
    public float recoilDuration = 0.1f;
    [Header("摄像机返回的时间")]
    public float returnDuration = 0.3f;
    
    [Space]
    [Header("摄像机震动参数")]
    public float shakeStrength = 0.5f;
    public int shakeVibrato = 10;
    public float shakeRandomness = 10f;
    
    [Space]
    public Ease gunAnimationMode =  Ease.OutCubic;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    
    
    // 存储当前正在播放的动画
    private Sequence currentShootSequence;

    private void Start()
    {
        // 设置相机的原始位置和角度为初始时相机的位置和角度
        originalCameraPosition = playerCamera.transform.localPosition;
        originalCameraRotation = playerCamera.transform.localRotation;
        playerCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 执行射击行为
            Shoot();
        }
    }

    private void Shoot()
    {
        // 如果当前存在动画，并且不为空，则将其动画删除
        if (currentShootSequence is not null && currentShootSequence.IsPlaying())
        {
            // 暂停当前 Sequence 上的所有动画
            currentShootSequence.Kill();
        }
        
        
        // 生成当前伤害值
        // 注意：这里使用的 Range 是 UnityEngine 当中的方法
        int damage = Random.Range(50,101);

        // 利用 DOTween 自带的方法，创建一个空的序列
        currentShootSequence = DOTween.Sequence();

        // ------------ 阶段一：生成后坐力效果 ------------------
        // 为当前的动画开始添加回调
        currentShootSequence.AppendCallback(() =>
        {
            Debug.Log("执行开枪");
        });
        
        // 摄像机向上并旋转
        // 参数一：当前旋转的角度大小
        // 参数二：执行旋转所需要的时间
        // 参数三：旋转的模式
        Tweener recoilRotate = playerCamera.transform.DOLocalRotate(
            new Vector3(-recoilUpwardForce,0,0),
            recoilDuration,
            RotateMode.LocalAxisAdd
            ).SetEase(gunAnimationMode);
        
        // 摄像机轻微后退
        Tweener recoilMove = playerCamera.transform.DOLocalMoveZ(
            originalCameraPosition.z - CameraMoveZ,
            recoilDuration
            ).SetEase(gunAnimationMode);
        
        Tweener cameraShake = playerCamera.transform.DOShakePosition(
            0.2f,
            shakeStrength,
            shakeVibrato,
            shakeRandomness,
            false, // 是否以整数值来更改位置，物体的位置跳跃会呈现卡顿感
            true // 震动结束时是否会有淡出效果，如果为false，则震动戛然而止
            ).SetEase(gunAnimationMode);
        
        // 同时执行后坐力移动和旋转
        currentShootSequence.Join(recoilRotate);
        currentShootSequence.Join(recoilMove);
        currentShootSequence.Join(cameraShake);
        
        // ------------ 阶段二：恢复屏幕位置 ------------------
        Tweener returnRotate = playerCamera.transform.DOLocalRotate(
            Vector3.zero,
            returnDuration
            ).SetEase(Ease.OutElastic);

        Tweener returnMove = playerCamera.transform.DOMove(
            originalCameraPosition,
            returnDuration
            ).SetEase(Ease.OutElastic);

        currentShootSequence.Append(returnRotate);
        currentShootSequence.Join(returnMove);
        currentShootSequence.AppendCallback(() =>
        {
            // 在所有动画的最后，添加一个回调
        });

        currentShootSequence.OnStart(() =>
        {
            Debug.Log("射击动画开始");
        });

        currentShootSequence.OnComplete(() =>
        {
            Debug.Log("射击动画结束");
            currentShootSequence = null;
        });

        currentShootSequence.OnKill(()=>
        {
            // 重置摄像机位置？不需要吧，已经重置了    
        });
    }

    private void OnDestroy()
    {
        if (currentShootSequence is not null)
        {
            currentShootSequence.Kill();
            currentShootSequence = null;
        }
    }
}