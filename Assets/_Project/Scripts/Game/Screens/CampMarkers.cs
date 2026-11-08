using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // docs/ui/stage-select.md 4: region markers are screen UI pinned to the map's 3D points. Built once; each frame only moves them.
    public sealed class CampMarkers : MonoBehaviour
    {
        const float Hit = 96f, Badge = 44f, Picked = 56f, Band = 280f;

        sealed class Marker
        {
            public RectTransform Root, Badge, NameBand;
            public Image Ring, Fill, Star;
            public Text Number, Name;
            public bool Hover;
        }

        Marker[] markers;
        CampaignMapView map;
        RectTransform canvas;
        Rect empty;
        CampLayer layer;
        int focus;

        public static CampMarkers Build(RectTransform screen, ArtCatalog art, CampaignMapView map, Action<int> pick)
        {
            var host = Ui.Node(screen, "Markers");
            Ui.Stretch(host);
            var view = host.gameObject.AddComponent<CampMarkers>();
            view.map = map;
            view.canvas = screen;
            var kit = new Kit(art);
            view.kit = kit;
            view.markers = new Marker[Campaign.Stages.Length];
            for (var i = 0; i < view.markers.Length; i++)
            {
                var m = new Marker();
                m.Root = Ui.Node(host, "Marker " + (i + 1));
                m.Root.anchorMin = m.Root.anchorMax = Vector2.zero;
                m.Root.sizeDelta = new Vector2(Hit, Hit);
                var hit = Ui.Bg(m.Root, Ui.Alpha(Ui.Ink, 0f));
                hit.raycastTarget = true;
                m.Badge = Ui.Place(m.Root, "Badge", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Badge, Badge));
                m.Fill = Ui.Sprite(m.Badge, "Fill", kit.S("SPR_DarkFantasyMenus_Frame_Ring_01_Background"), Ui.Deep);
                Ui.Stretch(m.Fill.rectTransform);
                m.Ring = Ui.Sprite(m.Badge, "Ring", kit.S("SPR_DarkFantasyMenus_Frame_Ring_01"), Ui.Line);
                Ui.Stretch(m.Ring.rectTransform, -2, -2, -2, -2);
                m.Number = Kit.T(m.Badge, (i + 1).ToString(), 20, Ui.Bone, true, false, TextAnchor.MiddleCenter);
                Ui.Stretch(m.Number.rectTransform);
                m.Star = Ui.Sprite(m.Root, "Star", kit.S(Kit.Icon.Star), Ui.Line);
                var star = m.Star.rectTransform;
                star.anchorMin = star.anchorMax = new Vector2(0.5f, 0.5f);
                star.anchoredPosition = new Vector2(18f, -18f);
                star.sizeDelta = new Vector2(20f, 20f);
                m.NameBand = Ui.Place(m.Root, "Name", new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(Band, 32f));
                m.NameBand.pivot = new Vector2(0.5f, 1f);
                Ui.Bg(m.NameBand, Ui.Alpha(Ui.Deep, 0.92f), Ui.Line, false);
                m.Name = Kit.T(m.NameBand, Loc.Text(Campaign.Stages[i].Title), 17, Ui.Bone, false, false, TextAnchor.MiddleCenter);
                Ui.Stretch(m.Name.rectTransform);
                var index = i;
                var btn = m.Root.gameObject.AddComponent<Button>();
                btn.targetGraphic = hit;
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => pick(index));
                var hover = m.Root.gameObject.AddComponent<MarkerHover>();
                hover.Changed = on => { m.Hover = on; view.Restyle(); };
                view.markers[i] = m;
            }
            return view;
        }

        Campaign campaign;
        Kit kit;

        // empty is in normalized viewport units (stage-select.md 2-3).
        public void Fill(Campaign c, CampNav nav, Rect empty)
        {
            campaign = c;
            layer = nav.Layer;
            focus = nav.Stage;
            this.empty = empty;
            for (var i = 0; i < markers.Length; i++)
            {
                var m = markers[i];
                var last = i == markers.Length - 1;
                var s = Campaign.Stages[i];
                var accent = last ? Ui.Blood : Ui.Steel;
                // Fx keeps the colour it was added with, so a refill drops the old pulse before recolouring.
                foreach (var fx in m.Ring.GetComponents<Fx>()) { if (Application.isPlaying) Destroy(fx); else DestroyImmediate(fx); }
                if (c.IsCleared(s)) { m.Ring.color = accent; m.Fill.color = accent; m.Number.color = Ui.Ink; }
                else if (c.StageOpen(i)) { m.Ring.color = last ? Ui.Blood : Ui.Sun; m.Fill.color = Ui.Deep; m.Number.color = Ui.Bone; Fx.Pulse(m.Ring); }
                else { m.Ring.color = last ? Ui.BloodDim : Ui.Line; m.Fill.color = Ui.Deep; m.Number.color = Ui.Dim; }
                var locked = !c.StageOpen(i);
                m.Star.sprite = kit.S(locked ? Kit.Icon.Lock : Kit.Icon.Star);
                m.Star.rectTransform.sizeDelta = Vector2.one * (locked ? 18f : 20f);
                m.Star.color = locked ? Ui.Dim : c.BonusDone.Contains(s.Id) ? Ui.Candle : Ui.Line;
                m.Name.color = last ? Ui.Blood : Ui.Bone;
            }
            Restyle();
            LateUpdate();
        }

        // On the map only the hovered marker shows its name; the keyboard focus just lifts its badge. Info pins the picked stage's name.
        // The spec's 4-2 also bands the focus; the user narrowed it (docs/areas/ui.md decisions, #47).
        void Restyle()
        {
            if (markers == null) return;
            for (var i = 0; i < markers.Length; i++)
            {
                var m = markers[i];
                var picked = layer == CampLayer.Info && i == focus;
                var focused = layer == CampLayer.Map && i == focus;
                m.NameBand.gameObject.SetActive(m.Hover || picked);
                m.Badge.sizeDelta = Vector2.one * (picked ? Picked : Badge);
                m.Ring.transform.localScale = Vector3.one * ((m.Hover || focused) && !picked ? 1.1f : 1f);
            }
        }

        void LateUpdate()
        {
            if (markers == null || map == null) return;
            var size = canvas.rect.size;
            for (var i = 0; i < markers.Length; i++)
            {
                var m = markers[i];
                var p = layer == CampLayer.Barracks ? null : map.ScreenPoint(i);
                var show = p.HasValue && empty.Contains(p.Value);
                if (m.Root.gameObject.activeSelf != show) m.Root.gameObject.SetActive(show);
                if (show) m.Root.anchoredPosition = new Vector2(p.Value.x * size.x, p.Value.y * size.y);
            }
            // 4-3: far markers (higher on screen) first, hovered/focused/picked on top.
            Array.Sort(order ??= BuildOrder(), (a, b) => Rank(b).CompareTo(Rank(a)));
            for (var k = 0; k < order.Length; k++) markers[order[k]].Root.SetSiblingIndex(k);
        }

        int[] order;

        int[] BuildOrder()
        {
            var o = new int[markers.Length];
            for (var i = 0; i < o.Length; i++) o[i] = i;
            return o;
        }

        float Rank(int i)
        {
            var m = markers[i];
            var top = m.Hover || i == focus ? -10000f : 0f;
            return m.Root.anchoredPosition.y + top;
        }

        sealed class MarkerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Action<bool> Changed;
            public void OnPointerEnter(PointerEventData e) => Changed?.Invoke(true);
            public void OnPointerExit(PointerEventData e) => Changed?.Invoke(false);
        }
    }
}
