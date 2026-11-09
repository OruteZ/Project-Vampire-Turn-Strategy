using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VampireSpawn.Core;

namespace VampireSpawn.Game.Screens
{
    // docs/ui/stage-select.md 4: region markers are screen UI pinned to the map's 3D points. Built once; each frame only moves them.
    // After CinemachineBrain (LateUpdate) has placed the camera, so markers never trail it by a frame.
    [DefaultExecutionOrder(1000)]
    public sealed class CampMarkers : MonoBehaviour
    {
        const float Badge = 44f, Picked = 56f;

        sealed class Marker
        {
            public VisualElement Root, Badge, Ring, Fill, Star;
            public Label Number, Name;
            public IVisualElementScheduledItem Pulse;
            public bool Hover;
            public float Size = CampMarkers.Badge;
        }

        Marker[] markers;
        CampaignMapView map;
        ArtCatalog art;
        VisualElement layer;
        Rect empty;
        CampLayer shownLayer;
        int focus;

        public static CampMarkers Build(Transform host, VisualElement layer, ArtCatalog art, CampaignMapView map, Action<int> pick)
        {
            var view = host.gameObject.AddComponent<CampMarkers>();
            view.map = map;
            view.art = art;
            view.layer = layer;
            view.markers = new Marker[Campaign.Stages.Length];
            for (var i = 0; i < view.markers.Length; i++)
            {
                var m = new Marker { Root = ToolkitKit.E(layer, "marker", true) };
                m.Root.name = "Marker " + (i + 1);
                m.Badge = ToolkitKit.RingBadge(m.Root, art, (i + 1).ToString(), Badge, Ui.Line, Ui.Deep, Ui.Bone);
                m.Badge.AddToClassList("marker-badge");
                m.Fill = m.Badge.Q(className: "ring-fill");
                m.Ring = m.Badge.Q(className: "ring");
                m.Number = m.Badge.Q<Label>();
                m.Star = ToolkitKit.E(m.Root, "marker-star");
                m.Name = ToolkitKit.L(m.Root, Loc.Text(Campaign.Stages[i].Title), "marker-name");
                var index = i;
                ToolkitKit.Click(m.Root, () => pick(index));
                m.Root.RegisterCallback<PointerEnterEvent>(_ => { m.Hover = true; view.Restyle(); });
                m.Root.RegisterCallback<PointerLeaveEvent>(_ => { m.Hover = false; view.Restyle(); });
                view.markers[i] = m;
            }
            return view;
        }

        // empty is in normalized viewport units (stage-select.md 2-3).
        public void Fill(Campaign c, CampNav nav, Rect empty)
        {
            shownLayer = nav.Layer;
            focus = nav.Stage;
            this.empty = empty;
            for (var i = 0; i < markers.Length; i++)
            {
                var m = markers[i];
                var last = i == markers.Length - 1;
                var s = Campaign.Stages[i];
                var accent = last ? Ui.Blood : Ui.Steel;
                m.Pulse?.Pause();
                m.Pulse = null;
                Color ring, fill, ink;
                if (c.IsCleared(s)) (ring, fill, ink) = (accent, accent, Ui.Ink);
                else if (c.StageOpen(i)) (ring, fill, ink) = (last ? Ui.Blood : Ui.Sun, Ui.Deep, Ui.Bone);
                else (ring, fill, ink) = (last ? Ui.BloodDim : Ui.Line, Ui.Deep, Ui.Dim);
                m.Ring.style.unityBackgroundImageTintColor = ring;
                m.Fill.style.unityBackgroundImageTintColor = fill;
                m.Number.style.color = ink;
                if (c.StageOpen(i) && !c.IsCleared(s)) m.Pulse = Fx.Pulse(m.Ring, ring);
                var locked = !c.StageOpen(i);
                ToolkitKit.Picture(m.Star, art.Ui(locked ? Kit.Icon.Lock : Kit.Icon.Star), locked ? Ui.Dim : c.BonusDone.Contains(s.Id) ? Ui.Candle : Ui.Line);
                m.Star.EnableInClassList("locked", locked);
                m.Name.style.color = last ? Ui.Blood : Ui.Bone;
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
                var picked = shownLayer == CampLayer.Info && i == focus;
                var focused = shownLayer == CampLayer.Map && i == focus;
                m.Name.EnableInClassList("shown", m.Hover || picked);
                m.Size = picked ? Picked : Badge;
                m.Ring.style.scale = new Scale(Vector2.one * ((m.Hover || focused) && !picked ? 1.1f : 1f));
            }
        }

        void LateUpdate()
        {
            if (markers == null || map == null) return;
            var size = ToolkitKit.PanelSize();
            for (var i = 0; i < markers.Length; i++)
            {
                var m = markers[i];
                var p = shownLayer == CampLayer.Barracks ? null : map.ScreenPoint(i);
                var show = p.HasValue && empty.Contains(p.Value);
                m.Root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (show)
                {
                    m.Root.style.left = p.Value.x * size.x;
                    m.Root.style.top = (1f - p.Value.y) * size.y;
                }
                // stage-select.md 4-2: 44 -> 56 over 0.15 s.
                var d = Mathf.MoveTowards(m.Badge.style.width.value.value, m.Size, (Picked - Badge) / 0.15f * Time.unscaledDeltaTime);
                m.Badge.style.width = d;
                m.Badge.style.height = d;
            }
            // 4-3: far markers (higher on screen) first, hovered/focused/picked on top.
            layer.Sort((a, b) => Rank(Array.FindIndex(markers, x => x.Root == a)).CompareTo(Rank(Array.FindIndex(markers, x => x.Root == b))));
        }

        float Rank(int i)
        {
            var m = markers[i];
            var top = m.Hover || i == focus ? 10000f : 0f;
            return m.Root.style.top.value.value + top;
        }
    }
}
