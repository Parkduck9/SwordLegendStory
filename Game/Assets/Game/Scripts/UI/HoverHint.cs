using UnityEngine;
using UnityEngine.EventSystems;

namespace SwordPrototype.UI
{
    /// <summary>마우스를 올리면 설명을 알려 주는 표시(스탯 배분의 + 버튼 등).</summary>
    public sealed class HoverHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string hint;
        public static string Current { get; private set; }
        public void OnPointerEnter(PointerEventData eventData) => Current = hint;
        public void OnPointerExit(PointerEventData eventData) { if (Current == hint) Current = null; }
    }
}
