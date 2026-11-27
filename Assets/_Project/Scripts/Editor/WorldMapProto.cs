using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VampireSpawn.Editor
{
    // Throwaway (#152): shots of the copied demo scene to pick the map view and the castle-interior reveal. Never saves the scene.
    public static class WorldMapProto
    {
        public const string Source = "Assets/Synty/PolygonDarkFantasy/Scenes/Demo_Cathedral_01.unity";
        public const string Copy = "Assets/_Project/Scenes/World.unity";

        public static string MakeCopy()
        {
            if (File.Exists(Copy)) return "있음 " + Copy;
            return AssetDatabase.CopyAsset(Source, Copy) ? "복사 " + Copy : "실패";
        }

        // Every variant to compare (#152): the road face, the castle face closed / cut above the galleries / cut to the ground floor, and a camera inside.
        public static string All(string dir)
        {
            const string road = "69.4 -7.4 52.9;33.7 -8.3 45.1;16.3 -10.3 40.9;-0.1 -7.5 35.0;15.5 -5.5 32.8;34.0 -4.9 14.7;-13.1 -8.5 38.5;-16.8 -2.2 14.3;2.3 -2.0 11.0;-1.0 3.4 -3.2";
            const string castle = "-0.8 3.3 -3.6;-1.6 1.0 -13.8;-0.9 1.0 -21.8;-7.9 3.4 -16.0;8.9 2.2 -19.6;11.5 4.3 -27.4;-9.6 4.2 -27.4;0.0 1.0 -31.9;-0.1 1.0 -39.7;-0.7 2.5 -44.0";
            return string.Join(" | ",
                Shot(dir + "/p1-road.png", 95f, 30f, 105f, 16f, 222f, 45f, marks: road, fogEnd: 3000f, fill: 0.6f),
                Shot(dir + "/p1b-road-stage4.png", 25.7f, 2f, 63.6f, 16f, 222f, 45f, marks: road, fogEnd: 3000f, fill: 0.6f),
                Shot(dir + "/p2a-closed.png", 40f, 70f, 12f, 51.5f, 228.8f, 40f, marks: castle, first: 11, fogEnd: 3000f, fill: 0.8f),
                Shot(dir + "/p2b-gallery.png", 40f, 70f, 12f, 51.5f, 228.8f, 40f, cut: 12.5f, marks: castle, first: 11, fogEnd: 3000f, fill: 0.8f),
                Shot(dir + "/p2c-floor1.png", 40f, 70f, 12f, 51.5f, 228.8f, 40f, cut: 5f, marks: castle, first: 11, fogEnd: 3000f, fill: 0.8f),
                Shot(dir + "/p2d-floor1-stage18.png", 15f, 26.1f, -18.9f, 51.5f, 228.8f, 40f, cut: 5f, marks: castle, first: 11, fogEnd: 3000f, fill: 0.8f),
                Shot(dir + "/p3-inside.png", 0f, 7f, -3f, 8f, 180f, 60f, fogEnd: 3000f));
        }

        // cut: hides cathedral pieces whose centre is above this height (<= -999 hides nothing).
        // keep: comma-separated Cathedral child groups that ignore the cut and use their lowest point instead (floors, props).
        // marks: "x y z;x y z" numbered from first.
        public static string Shot(string path, float x, float y, float z, float pitch, float yaw, float fov, float ortho = 0f, float cut = -999f, string low = "Floors,Props,Lighting", string marks = "", int first = 1, float fogEnd = 0f, float fill = 0f, int w = 1920, int h = 1080)
        {
            if (EditorSceneManager.GetActiveScene().path != Copy) EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var scene = EditorSceneManager.GetActiveScene();
            var cam = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First();
            var pose = (cam.transform.position, cam.transform.rotation, cam.fieldOfView, cam.orthographic, cam.orthographicSize, cam.aspect);
            var fog = RenderSettings.fogEndDistance;
            if (fogEnd > 0f) RenderSettings.fogEndDistance = fogEnd;
            var hidden = new List<Renderer>();
            var made = new List<GameObject>();
            try
            {
                if (cut > -999f)
                {
                    var lows = low.Split(',');
                    var cathedral = scene.GetRootGameObjects().First(r => r.name == "Cathedral").transform;
                    foreach (Transform group in cathedral)
                        foreach (var r in group.GetComponentsInChildren<Renderer>())
                        {
                            var at = lows.Contains(group.name) ? r.bounds.min.y : r.bounds.center.y;
                            if (r.enabled && at > cut) { r.enabled = false; hidden.Add(r); }
                        }
                }
                cam.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(pitch, yaw, 0f));
                cam.fieldOfView = fov;
                cam.orthographic = ortho > 0f;
                if (ortho > 0f) cam.orthographicSize = ortho;
                cam.aspect = (float)w / h;
                if (fill > 0f)
                {
                    var light = new GameObject("Fill") { hideFlags = HideFlags.DontSave }.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.color = new Color(0.75f, 0.8f, 1f);
                    light.intensity = fill;
                    light.shadows = LightShadows.None;
                    light.transform.rotation = Quaternion.Euler(Mathf.Max(pitch, 35f) + 10f, yaw - 25f, 0f);
                    made.Add(light.gameObject);
                }
                var n = first;
                foreach (var m in marks.Split(';').Where(s => s.Trim().Length > 0))
                {
                    var v = m.Trim().Split(' ').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                    made.Add(Mark(new Vector3(v[0], v[1], v[2]), n++, cam));
                }
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                Render(cam, w, h, path);
                return path + " hidden=" + hidden.Count;
            }
            finally
            {
                RenderSettings.fogEndDistance = fog;
                foreach (var r in hidden) r.enabled = true;
                foreach (var g in made) Object.DestroyImmediate(g);
                cam.ResetAspect();
                (cam.transform.position, cam.transform.rotation, cam.fieldOfView, cam.orthographic, cam.orthographicSize) = (pose.Item1, pose.Item2, pose.Item3, pose.Item4, pose.Item5);
            }
        }

        // Viewport points "u v;u v" of a shot -> the world points they hit, as a marks string.
        public static string Pick(float x, float y, float z, float pitch, float yaw, float fov, string points, float cut = -999f, int w = 1920, int h = 1080)
        {
            if (EditorSceneManager.GetActiveScene().path != Copy) EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var scene = EditorSceneManager.GetActiveScene();
            var cam = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First();
            var pose = (cam.transform.position, cam.transform.rotation, cam.fieldOfView);
            try
            {
                cam.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(pitch, yaw, 0f));
                cam.fieldOfView = fov;
                cam.aspect = (float)w / h;
                Physics.SyncTransforms();
                var result = new List<string>();
                foreach (var m in points.Split(';').Where(t => t.Trim().Length > 0))
                {
                    var v = m.Trim().Split(' ').Select(t => float.Parse(t, CultureInfo.InvariantCulture)).ToArray();
                    var ray = cam.ViewportPointToRay(new Vector3(v[0], v[1], 0f));
                    var hits = Physics.RaycastAll(ray, 600f).Where(k => cut <= -999f || k.point.y <= cut).OrderBy(k => k.distance).ToArray();
                    var at = hits.Length > 0 ? hits[0].point : ray.GetPoint(100f);
                    result.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.0} {1:0.0} {2:0.0}", at.x, at.y, at.z) + (hits.Length > 0 ? "" : " !miss"));
                }
                return string.Join(";", result);
            }
            finally
            {
                cam.ResetAspect();
                (cam.transform.position, cam.transform.rotation, cam.fieldOfView) = pose;
            }
        }

        // World bounds of every top-level group and every child of the named roots, to aim shots.
        public static string Bounds(string roots = "Cathedral,Environment")
        {
            if (EditorSceneManager.GetActiveScene().path != Copy) EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var open = roots.Split(',');
            var lines = new List<string>();
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                lines.Add(Line(root.transform, ""));
                if (open.Contains(root.name)) foreach (Transform c in root.transform) lines.Add(Line(c, "  "));
            }
            return string.Join("\n", lines);
        }

        static string Line(Transform t, string pad)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return pad + t.name + " (active " + t.gameObject.activeInHierarchy + ") -";
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return $"{pad}{t.name} (active {t.gameObject.activeInHierarchy}) n={rs.Length} x[{b.min.x:0},{b.max.x:0}] y[{b.min.y:0},{b.max.y:0}] z[{b.min.z:0},{b.max.z:0}]";
        }

        static GameObject Mark(Vector3 at, int n, Camera cam)
        {
            var root = new GameObject("Mark " + n) { hideFlags = HideFlags.DontSave };
            root.transform.position = at;
            var d = cam.orthographic ? cam.orthographicSize * 2f : Vector3.Distance(cam.transform.position, at) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f;
            var text = new GameObject("Text").AddComponent<TextMesh>();
            text.transform.SetParent(root.transform, false);
            text.transform.rotation = cam.transform.rotation;
            text.text = n.ToString();
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 96;
            text.fontStyle = FontStyle.Bold;
            text.characterSize = d * 0.0045f;
            text.color = new Color(1f, 0.95f, 0.6f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return root;
        }

        static void Render(Camera cam, int w, int h, string path)
        {
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = null;
                rt.Release();
            }
        }
    }
}
