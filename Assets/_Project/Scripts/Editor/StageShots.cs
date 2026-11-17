using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VampireSpawn.Core;
using VampireSpawn.Game;
using Stage = VampireSpawn.Core.Stage;

namespace VampireSpawn.Editor
{
    // Battle-camera and top-down shots of every stage for the dressing feedback tool (docs/art/stage-dressing.md 7).
    public static class StageShots
    {
        const int TopPx = 1600, TopMargin = 6;

        // ids: comma-separated stage ids, empty = every campaign stage. Writes <dir>/<id>.battle.png, <id>.top.png, <id>.json.
        public static string Capture(string dir, string ids = "")
        {
            var art = AssetDatabase.LoadAssetAtPath<ArtCatalog>("Assets/_Project/Art/ArtCatalog.asset");
            var want = ids.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            var stages = Campaign.Stages.Where(s => want.Length == 0 || want.Contains(s.Id)).ToArray();
            Directory.CreateDirectory(dir);
            var fog = (RenderSettings.fog, RenderSettings.fogColor, RenderSettings.ambientMode, RenderSettings.ambientLight);
            try
            {
                foreach (var stage in stages) One(art, stage, dir);
                return dir + " " + string.Join(",", stages.Select(s => s.Id));
            }
            finally
            {
                (RenderSettings.fog, RenderSettings.fogColor, RenderSettings.ambientMode, RenderSettings.ambientLight) = fog;
            }
        }

        static void One(ArtCatalog art, Stage stage, string dir)
        {
            // DontSave and far from the origin: the user's open scene is neither changed nor in the shot.
            var root = new GameObject("StageShots") { hideFlags = HideFlags.DontSave };
            root.transform.position = new Vector3(5000f, 0f, 0f);
            try
            {
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.transform.SetParent(root.transform, false);
                sun.type = LightType.Directional;
                // Battle.unity's directional light.
                sun.color = new Color(0.561f, 0.643f, 0.8f);
                sun.intensity = 0.8f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = new Quaternion(0.483f, -0.224f, 0.129f, 0.837f);

                var cam = new GameObject("Camera").AddComponent<Camera>();
                cam.transform.SetParent(root.transform, false);
                cam.fieldOfView = 40f;
                cam.clearFlags = CameraClearFlags.SolidColor;

                var boardGo = new GameObject("Board");
                boardGo.transform.SetParent(root.transform, false);
                var board = boardGo.AddComponent<BoardView>();
                board.art = art;
                board.viewCamera = cam;
                board.Show(stage);

                cam.aspect = 16f / 9f;
                board.FrameInto(new Rect(0f, 0f, 1f, 1f));
                Shot(cam, 1280, 720, Path.Combine(dir, stage.Id + ".battle.png"));

                var a = BoardView.Local(new Hex(0, 0), art.hexSize);
                var b = BoardView.Local(new Hex(board.Board.Cols - 1, board.Board.Rows - 1), art.hexSize);
                var m = TopMargin * art.hexSize;
                float minX = a.x - m, maxX = b.x + m, minZ = b.z - m, maxZ = a.z + m;
                var w = maxX - minX; var h = maxZ - minZ;
                var px = w >= h ? TopPx : Mathf.RoundToInt(TopPx * w / h);
                var py = w >= h ? Mathf.RoundToInt(TopPx * h / w) : TopPx;
                cam.ResetProjectionMatrix();
                cam.orthographic = true;
                cam.orthographicSize = h * 0.5f;
                cam.aspect = w / h;
                cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cam.transform.position = boardGo.transform.TransformPoint(new Vector3((minX + maxX) * 0.5f, 60f, (minZ + maxZ) * 0.5f));
                RenderSettings.fog = false;
                Shot(cam, px, py, Path.Combine(dir, stage.Id + ".top.png"));

                // Lets the tool turn a pixel of the top shot into a cell (board-local, stage-dressing.md 4-4).
                File.WriteAllText(Path.Combine(dir, stage.Id + ".json"), JsonUtility.ToJson(new Meta
                {
                    id = stage.Id, title = Loc.Text(stage.Title), cols = board.Board.Cols, rows = board.Board.Rows, hexSize = art.hexSize,
                    minX = minX, maxX = maxX, minZ = minZ, maxZ = maxZ, map = stage.Map,
                }));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [System.Serializable]
        class Meta
        {
            public string id, title;
            public int cols, rows;
            public float hexSize, minX, maxX, minZ, maxZ;
            public string[] map;
        }

        static void Shot(Camera cam, int w, int h, string path)
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
