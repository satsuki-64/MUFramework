using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DOTweenTest2 : MonoBehaviour
{
    [Header("UI组件")]
    public Text titleText;
    public Text scoreText;
    
    [Header("摄像机")]
    public Camera mainCamera;
    
    [Header("动画参数")]
    public float sequenceDuration = 3f;
    public Vector3 cameraTargetPosition = new Vector3(0, 5, -10);
    public Vector3 cameraTargetRotation = new Vector3(30, 0, 0);
    
    /// <summary>
    /// 如果要创建复杂的动画效果，可以先创建一个 Sequence 对象
    /// </summary>
    private Sequence mainSequence;
    private int currentScore = 0;

    void Start()
    {
        mainCamera = Camera.main;
        
        // 初始化状态
        InitializeAnimationState();
        
        // 创建主动画序列
        CreateMainAnimationSequence();
        
        // 播放动画
        mainSequence.Play();
    }

    void InitializeAnimationState()
    {
        // 设置文本初始状态（完全透明）
        titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, 0);
        scoreText.color = new Color(scoreText.color.r, titleText.color.g, titleText.color.b, 0);
        
        // 设置文本初始位置（屏幕左侧外）
        titleText.rectTransform.anchoredPosition = new Vector2(-800, 0);
        scoreText.rectTransform.anchoredPosition = new Vector2(800, -200);
        
        // 设置初始缩放
        titleText.rectTransform.localScale = Vector3.zero;
        scoreText.rectTransform.localScale = Vector3.zero;
    }

    void CreateMainAnimationSequence()
    {
        // 创建主序列
        // DOTween.Sequence() 方法会创建一个新的 Sequence 实例
        mainSequence = DOTween.Sequence();
        
        // === 第一阶段：标题入场动画 ===
        mainSequence.AppendCallback(() => Debug.Log("=== 动画开始：标题入场 ==="));
        
        // 标题从左侧滑入并淡入
        // 三个动画是并行播放的
        mainSequence.Append(titleText.rectTransform.DOAnchorPosX(0, 0.8f).SetEase(Ease.OutBack));
        mainSequence.Join(titleText.DOFade(1, 0.8f)); // 同时淡入
        mainSequence.Join(titleText.rectTransform.DOScale(1.2f, 0.4f).SetEase(Ease.OutBounce)); // 放大为1.2倍
        
        // 标题缩放回正常大小
        mainSequence.Append(titleText.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.InOutSine));
        
        // === 第二阶段：摄像机移动动画 ===
        mainSequence.AppendCallback(() => Debug.Log("=== 第二阶段：摄像机移动 ==="));
        
        // 摄像机平滑移动到新位置
        mainSequence.Append(mainCamera.transform.DOMove(cameraTargetPosition, 1.5f).SetEase(Ease.InOutCubic));
        mainSequence.Join(mainCamera.transform.DORotate(cameraTargetRotation, 1.5f).SetEase(Ease.InOutCubic));
        
        // 同时进行摄像机视野变化
        mainSequence.Join(mainCamera.DOFieldOfView(70f, 1.5f));
        
        // === 第三阶段：分数显示动画 ===
        mainSequence.AppendCallback(() => 
        {
            Debug.Log("=== 第三阶段：分数显示 ===");
            currentScore = 1000; // 设置分数值
            scoreText.text = "得分: " + currentScore.ToString();
        });
        
        // 分数文本从右侧飞入
        mainSequence.Append(scoreText.rectTransform.DOAnchorPosX(0, 0.6f).SetEase(Ease.OutBack));
        mainSequence.Join(scoreText.DOFade(1, 0.6f));
        mainSequence.Join(scoreText.rectTransform.DOScale(1f, 0.6f));
        
        // 分数数字滚动效果
        mainSequence.Join(DOTween.To(() => currentScore, x => 
        {
            currentScore = x;
            scoreText.text = "得分: " + currentScore.ToString();
        }, 10000, 2f).SetEase(Ease.OutQuad));
        
        // === 第四阶段：综合效果 ===
        mainSequence.AppendCallback(() => Debug.Log("=== 第四阶段：综合效果 ==="));
        
        // 标题抖动效果
        mainSequence.Append(titleText.rectTransform.DOShakeAnchorPos(0.5f, 10f, 10, 90f, false));
        
        // 分数文本颜色变化（金色闪光）
        mainSequence.Join(scoreText.DOColor(Color.yellow, 0.3f));
        mainSequence.Append(scoreText.DOColor(Color.white, 0.3f));
        
        // 摄像机轻微震动
        mainSequence.Join(mainCamera.DOShakePosition(0.3f, 0.5f, 10, 90f));
        
        // === 最终阶段：循环动画 ===
        mainSequence.AppendCallback(() => Debug.Log("=== 最终阶段：开始循环动画 ==="));
        
        // 创建分数文本的呼吸效果（循环）
        Sequence breathSequence = DOTween.Sequence();
        breathSequence.Append(scoreText.rectTransform.DOScale(1.1f, 0.8f).SetEase(Ease.InOutSine));
        breathSequence.Append(scoreText.rectTransform.DOScale(1f, 0.8f).SetEase(Ease.InOutSine));
        breathSequence.SetLoops(-1, LoopType.Yoyo);
        
        // 将呼吸效果加入到主序列
        mainSequence.AppendCallback(() => breathSequence.Play());
        
        // === 设置序列参数 ===
        mainSequence.SetAutoKill(false) // 不自动销毁，可以重复使用
                  .OnStart(OnAnimationStart) // 动画开始回调
                  .OnComplete(OnAnimationComplete) // 动画完成回调
                  .OnUpdate(OnAnimationUpdate); // 每帧更新回调
    }

    // === 回调函数 ===
    void OnAnimationStart()
    {
        Debug.Log("主动画序列开始播放");
        // 可以在这里播放开始音效
        // AudioManager.PlaySound("AnimationStart");
    }

    void OnAnimationComplete()
    {
        Debug.Log("主动画序列播放完成");
        
        // 动画完成后，启动一个延迟回调
        DOVirtual.DelayedCall(2f, () => 
        {
            Debug.Log("动画完成后的延迟回调执行");
            // 可以在这里执行后续逻辑，比如加载下一个场景
        });
    }

    void OnAnimationUpdate()
    {
        // 每帧调用，可以在这里更新UI或检测状态
        // Debug.Log($"动画播放进度: {mainSequence.ElapsedPercentage():P2}");
    }

    // === 外部控制方法 ===
    public void RestartAnimation()
    {
        if (mainSequence != null)
        {
            mainSequence.Restart();
            Debug.Log("重新开始动画");
        }
    }

    public void PauseAnimation()
    {
        if (mainSequence != null)
        {
            mainSequence.Pause();
            Debug.Log("暂停动画");
        }
    }

    public void ResumeAnimation()
    {
        if (mainSequence != null)
        {
            mainSequence.Play();
            Debug.Log("继续播放动画");
        }
    }

    public void ChangeScore(int newScore)
    {
        // 动态改变分数值
        DOTween.To(() => currentScore, x => 
        {
            currentScore = x;
            scoreText.text = "得分: " + currentScore.ToString();
        }, newScore, 1f).SetEase(Ease.OutCubic);
    }

    void Update()
    {
        // 键盘控制示例
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartAnimation();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (mainSequence != null && mainSequence.IsPlaying())
            {
                PauseAnimation();
            }
            else
            {
                ResumeAnimation();
            }
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeScore(Random.Range(1000, 100000));
        }
    }

    void OnDestroy()
    {
        // 清理动画，防止内存泄漏
        if (mainSequence != null)
        {
            mainSequence.Kill();
        }
    }
}