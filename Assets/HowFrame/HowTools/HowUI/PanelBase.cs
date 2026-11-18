using UnityEngine;

namespace HowFrame
{
    /// <summary>
    /// 面板基类，生命周期由 UIManager 驱动：
    /// Init（首次创建）→ WhenLanguageChanged（首次创建及每次切换语言）→ WhenShow / WhenHide。
    /// </summary>
    public abstract class PanelBase : MonoBehaviour
    {
        protected internal abstract void Init();

        protected internal virtual void WhenShow()
        {
        }

        protected internal virtual void WhenHide()
        {
        }

        protected internal virtual void WhenShowWithParameter(object parameter)
        {
        }

        /// <summary>语言切换后调用（面板首次创建时也会调用一次），在这里刷新文本</summary>
        protected internal virtual void WhenLanguageChanged()
        {
        }
    }
}
