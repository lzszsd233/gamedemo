using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// 强制要求挂载这个脚本的物体必须有 Button 组件
[RequireComponent(typeof(Button))]
public class RebindButtonUI : MonoBehaviour
{
    [Header("挂了 RebindManager 的 Canvas")]
    public RebindManager rebindManager;

    [Header("负责改的动作")]
    public InputActionReference actionReference;

    [Header("负责改的键位")]
    public int bindingIndex = 0;

    [Header("文字显示器")]
    public TMP_Text buttonText;

    private void Start()
    {
        // 游戏刚开始时，把自己身上的文字改成当前真实的按键名
        if (actionReference != null && buttonText != null)
        {
            buttonText.text = actionReference.action.GetBindingDisplayString(bindingIndex);
        }

        // 代码绑定事件监听器
        Button myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        rebindManager.StartRebind(actionReference, bindingIndex, buttonText);
    }
}
