using UnityEngine;

namespace VampireSpawn.Game
{
    public sealed class Popup : MonoBehaviour
    {
        const float Lifetime = 0.9f, Rise = 0.6f;

        TextMesh text;
        float age, speed;
        Vector3 start;

        public static void Spawn(Vector3 at, string message, string style, float speed)
        {
            var go = new GameObject("Popup " + message);
            go.transform.position = at;
            var p = go.AddComponent<Popup>();
            p.start = at;
            p.speed = Mathf.Max(0.01f, speed);
            p.text = go.AddComponent<TextMesh>();
            p.text.text = message;
            p.text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = p.text.font.material;
            p.text.fontSize = 64;
            p.text.characterSize = 0.05f;
            p.text.anchor = TextAnchor.MiddleCenter;
            p.text.color = ColorOf(style);
        }

        static Color ColorOf(string style) => style switch
        {
            "blood" => Screens.Ui.Blood,
            "heal" => Screens.Ui.Sun,
            "spawn" => Screens.Ui.Spawn,
            _ => Screens.Ui.Bone,
        };

        void Update()
        {
            age += Time.deltaTime * speed;
            transform.position = start + Vector3.up * (Rise * age / Lifetime);
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
            var c = text.color;
            c.a = 1f - age / Lifetime;
            text.color = c;
            if (age >= Lifetime) Destroy(gameObject);
        }
    }
}
