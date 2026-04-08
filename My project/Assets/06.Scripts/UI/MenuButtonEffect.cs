using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

/// <summary>
/// UI 按钮选中动效（挂在每个按钮上）
/// 继承 ISelectHandler 和 IDeselectHandler 接口，监听键盘/手柄的选中状态
/// </summary>
public class MenuButtonEffect : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    private RectTransform rectTransform;
    private TMP_Text buttonText;

    [Header("动效参数")]
    public float hoverScale = 1.2f;    // 选中时放大 1.2 倍
    public float animationTime = 0.2f; // 动画速度（0.2秒极度丝滑）
    public Color hoverColor = Color.white;  // 选中时的发光色
    public Color normalColor = new Color(0.8f, 0.8f, 0.8f, 0.5f); // 未选中时的半透明灰色

    // 【全局广播站】：静态事件，当任何按钮被选中时，触发这个事件
    public static event System.Action<MenuButtonEffect> OnAnyButtonSelected;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        buttonText = GetComponentInChildren<TMP_Text>();

        // 初始化未选中的状态
        rectTransform.localScale = Vector3.one;
        if (buttonText != null) buttonText.color = normalColor;
    }

    private void OnEnable()
    {
        // 报名监听广播
        OnAnyButtonSelected += HandleOtherButtonSelected;
    }

    private void OnDisable()
    {
        // 退订广播
        OnAnyButtonSelected -= HandleOtherButtonSelected;

        // 按钮隐藏时，强行恢复原状（防止下次打开时还是巨大的）
        rectTransform.DOKill();
        if (buttonText != null) buttonText.DOKill();
        rectTransform.localScale = Vector3.one;
        if (buttonText != null) buttonText.color = normalColor;
    }

    /// <summary>
    /// 【核心 1】：当这个按钮被键盘或鼠标选中时
    /// </summary>
    public void OnSelect(BaseEventData eventData)
    {
        // 拿着大喇叭喊：“兄弟们，我被选中了！你们都退下！”
        // 这会触发所有其他按钮的 HandleOtherButtonSelected 方法
        OnAnyButtonSelected?.Invoke(this);

        // 1. 杀死旧动画
        rectTransform.DOKill();
        if (buttonText != null) buttonText.DOKill();

        // 2. 自己放大发光
        rectTransform.DOScale(hoverScale, animationTime).SetEase(Ease.OutBack);
        if (buttonText != null) buttonText.DOColor(hoverColor, animationTime);
    }

    /// <summary>
    /// 【核心 2】：鼠标放上来时，强行拿走焦点，这会自动触发上面的 OnSelect
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    /// <summary>
    /// 【核心 3】：当听到大喇叭喊“别人被选中了”时
    /// </summary>
    private void HandleOtherButtonSelected(MenuButtonEffect selectedButton)
    {
        // 如果被选中的人不是我，那我必须乖乖缩回去！
        if (selectedButton != this)
        {
            rectTransform.DOKill();
            if (buttonText != null) buttonText.DOKill();

            rectTransform.DOScale(1f, animationTime).SetEase(Ease.OutQuad);
            if (buttonText != null) buttonText.DOColor(normalColor, animationTime);
        }
    }
}