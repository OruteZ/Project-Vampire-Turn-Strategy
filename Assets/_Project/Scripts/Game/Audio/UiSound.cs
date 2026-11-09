using UnityEngine.UIElements;

namespace VampireSpawn.Game
{
    // docs/audio/sounds.md 4: hover and click for every button; a click on a .disabled one is denied. Button factories call Add once per button.
    public static class UiSound
    {
        public static void Add(VisualElement button)
        {
            button.RegisterCallback<PointerEnterEvent>(_ => { if (!button.ClassListContains("disabled")) Sound.Play(s => s.hover); });
            button.RegisterCallback<ClickEvent>(_ => Sound.Play(s => button.ClassListContains("disabled") ? s.denied : s.click));
        }
    }
}
