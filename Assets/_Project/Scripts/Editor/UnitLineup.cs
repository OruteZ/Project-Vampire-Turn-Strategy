using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Game;

namespace VampireSpawn.Editor
{
    // Every UnitLook built the way battle builds it: front idle, side idle, side attack (docs/art/unit-visuals.md 4).
    public static class UnitLineup
    {
        const float Gap = 1.7f, RowGap = 2.3f;

        static readonly (string state, float time, float yaw)[] Rows = { ("Idle", 0.4f, 180f), ("Idle", 0.4f, 90f), ("Attack", 0.25f, 90f) };

        public static string Capture(string path)
        {
            var art = AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/_Project/Art/ArtCatalog.asset");
            var looks = art.allyLooks.Concat(new[] { art.skeletonLook, art.vampireLook }).ToArray();
            // Far below the scene so the open board does not show up in the shot.
            var origin = new Vector3(0f, -500f, 0f);
            var root = new GameObject("UnitLineup") { hideFlags = HideFlags.DontSave };
            var rt = new RenderTexture(300 * looks.Length, 320 * Rows.Length, 24);
            try
            {
                for (var row = 0; row < Rows.Length; row++)
                for (var i = 0; i < looks.Length; i++)
                {
                    var at = origin + new Vector3(i * Gap, (Rows.Length - 1 - row) * RowGap, 0f);
                    var view = UnitView.Create(art, looks[i], root.transform, i, at, Rows[row].yaw, Color.gray, true);
                    var animator = view.GetComponentInChildren<Animator>();
                    if (animator == null || animator.runtimeAnimatorController == null) continue;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Play(Rows[row].state, 0, Rows[row].time);
                    animator.Update(0f);
                }

                var light = new GameObject("Light").AddComponent<Light>();
                light.transform.SetParent(root.transform, false);
                light.type = LightType.Directional;
                light.intensity = 1.4f;
                light.transform.rotation = Quaternion.Euler(40f, 150f, 0f);

                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.transform.SetParent(root.transform, false);
                cam.orthographic = true;
                cam.orthographicSize = Rows.Length * RowGap * 0.5f;
                cam.aspect = (float)rt.width / rt.height;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.22f, 0.22f, 0.25f);
                cam.transform.position = origin + new Vector3((looks.Length - 1) * Gap * 0.5f, Rows.Length * RowGap * 0.5f - 0.35f, -10f);
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                RenderTexture.active = null;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                return path + " " + string.Join(",", looks.Select(l => l.name));
            }
            finally
            {
                Object.DestroyImmediate(root);
                rt.Release();
            }
        }
    }
}
