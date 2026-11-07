using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VampireSpawn.Game.Screens
{
    // frame.tip tooltip (ui-style.md 5) shown after the pointer rests on the owner.
    public sealed class HoverTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        string text;
        float delay, showAt = -1f;
        RectTransform tip;

        public static void Add(RectTransform owner, string text, float delay)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!owner.TryGetComponent<Graphic>(out _)) Ui.Bg(owner, Ui.Alpha(Ui.Ink, 0f));
            var t = owner.gameObject.AddComponent<HoverTip>();
            t.text = text;
            t.delay = delay;
        }

        public void OnPointerEnter(PointerEventData e) => showAt = Time.unscaledTime + delay;

        public void OnPointerExit(PointerEventData e) => Hide();

        void OnDisable() => Hide();

        void OnDestroy() => Hide();

        void Hide()
        {
            showAt = -1f;
            if (tip != null) Destroy(tip.gameObject);
            tip = null;
        }

        void Update()
        {
            if (showAt < 0f || tip != null || Time.unscaledTime < showAt) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var root = (RectTransform)canvas.rootCanvas.transform;
            tip = Ui.Col(root, "Tip", 0, 10, Ui.Alpha(Ui.Deep, 0.96f), Ui.Line);
            tip.pivot = new Vector2(0.5f, 0f);
            tip.sizeDelta = new Vector2(320f, 0f);
            tip.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            tip.GetComponent<Image>().raycastTarget = false;
            Ui.Text(tip, text, 14f / Ui.Base, Ui.Muted);
            var corners = new Vector3[4];
            ((RectTransform)transform).GetWorldCorners(corners);
            tip.position = (corners[1] + corners[2]) / 2f + Vector3.up * 8f * canvas.rootCanvas.scaleFactor;
        }
    }

    public sealed class OnHover : MonoBehaviour, IPointerEnterHandler
    {
        System.Action enter;

        public static void Add(RectTransform owner, System.Action enter) => owner.gameObject.AddComponent<OnHover>().enter = enter;

        public void OnPointerEnter(PointerEventData e) => enter?.Invoke();
    }
}
