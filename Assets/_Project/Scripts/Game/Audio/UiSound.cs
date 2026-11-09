using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace VampireSpawn.Game
{
    // docs/audio/sounds.md 4: hover and click for every button. Button factories call Add once per button.
    public sealed class UiSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        Selectable target;

        public static T Add<T>(T button) where T : Selectable
        {
            button.gameObject.AddComponent<UiSound>().target = button;
            return button;
        }

        public static void Add(VisualElement button)
        {
            button.RegisterCallback<PointerEnterEvent>(_ => Sound.Play(s => s.hover));
            button.RegisterCallback<ClickEvent>(_ => Sound.Play(s => s.click));
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (target.interactable) Sound.Play(s => s.hover);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (target.interactable) Sound.Play(s => s.click);
            else Sound.Play(s => s.denied);
        }
    }
}
