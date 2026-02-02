using UnityEngine;
using UnityEngine.Events;

namespace MUFramework.UISystem.MVVMFramework
{
    public class ViewComponent : MonoBehaviour
    {
        private event UnityAction Reveal;
        private event UnityAction Hide;
        
        public void AddReveal(UnityAction action)
        {
            if (action != null)
            {
                Reveal = action;
            }
        }

        public void AddHide(UnityAction action)
        {
            if (action != null)
            {
                Hide = action;
            }
        }

        public void DoReveal()
        {
            Reveal?.Invoke();
        }

        public void DoHide()
        {
            Hide?.Invoke();
        }
    }
}