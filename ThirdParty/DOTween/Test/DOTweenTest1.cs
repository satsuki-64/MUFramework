using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DOTweenTest1 : MonoBehaviour
{
    public Text myText;
    private float _textAlpha; // 这个变量将用于控制透明度
    private Camera camera;

    private void Start()
    {
        camera = Camera.main;
        // camera.transform.DOJump(Vector3.up * 5, 1.5f, 5, 10);
        // camera.transform.DOMoveX(2, 10);
        // camera.transform.DOShakeRotation(2, 10);
        StartUIDOTween();
    }

    private void TextColor()
    {
        // 初始化，设置文本为完全不透明
        _textAlpha = 1f;
        UpdateTextColor(_textAlpha);

        // 使用 DOTween.To 对 _textAlpha 进行动画
        DOTween.To(
            () => _textAlpha,           // Getter: 获取当前alpha值 (1)
            x => { 
                _textAlpha = x;         // Setter: 将计算出的新值x赋给 _textAlpha
                UpdateTextColor(_textAlpha);      // 同时更新文本的实际颜色
            }, 
            0.1f,                          // 目标值: 半透明
            5f                          // 持续时间: 5秒
        );
        
        camera.transform.DOMoveX(2, 10);
    }

    // 一个辅助方法，根据 _textAlpha 更新文本颜色
    void UpdateTextColor(float textAlpha)
    {
        Color c = myText.color;
        c.a = textAlpha;
        myText.color = c;
    }
    
    public RectTransform myButtonRectTransform;
    
    void StartUIDOTween()
    {
        // 使用链式调用一气呵成地定义整个动画序列
        myButtonRectTransform.DOAnchorPos(Vector2.zero, 1.0f) // 1. 用1秒时间移动到屏幕中心
            .SetDelay(0.5f)                                    // 2. 等待0.5秒后开始
            .SetEase(Ease.OutBack)                             // 3. 使用带有回弹效果的缓动函数，让入场更有张力
            .SetLoops(2, LoopType.Yoyo)                         // 4. 循环2次（即入场后还会再弹出去一次）
            .OnComplete(() =>                                   // 5. 动画全部结束后
            { 
                Debug.Log("按钮入场动画播放完毕！");
                // 这里可以添加更多逻辑，比如启用按钮的交互功能
                camera.transform.DOMoveX(2, 10);
            });
    }
}