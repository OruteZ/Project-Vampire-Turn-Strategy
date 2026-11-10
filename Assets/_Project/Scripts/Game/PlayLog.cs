using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VampireSpawn.Core;

namespace VampireSpawn.Game
{
    // #113 plan 6: real seconds per turn (thinking vs enemy playback) and losses, one JSON file per battle in persistentDataPath/playlogs.
    public sealed class PlayLog
    {
        readonly string stage;
        readonly float started;
        float mark, deploy, player;
        bool inEnemy;
        readonly List<string> turns = new List<string>();

        public PlayLog(string stage)
        {
            this.stage = stage;
            started = mark = Time.realtimeSinceStartup;
        }

        static float Now => Time.realtimeSinceStartup;

        public void Started() { deploy = Now - mark; mark = Now; }

        public void EnemyBegins() { player = Now - mark; mark = Now; inEnemy = true; }

        public void EnemyEnds(Battle b)
        {
            if (!inEnemy) return;
            inEnemy = false;
            turns.Add($"{{\"turn\":{b.Turn - 1},\"player\":{player:F1},\"enemy\":{Now - mark:F1},\"lost\":{b.Stats.Lost},\"risen\":{b.Stats.Risen},\"moon\":{b.Moon}}}");
            mark = Now;
        }

        public string Finish(Battle b, bool won)
        {
            var dir = Path.Combine(Application.persistentDataPath, "playlogs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, stage + "-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            var json = new StringBuilder()
                .Append("{\"stage\":\"").Append(stage).Append("\",\"won\":").Append(won ? "true" : "false")
                .Append(",\"turns\":").Append(b.Turn).Append(",\"total\":").Append((Now - started).ToString("F1"))
                .Append(",\"deploy\":").Append(deploy.ToString("F1"))
                .Append(",\"lost\":").Append(b.Stats.Lost).Append(",\"executed\":").Append(b.Stats.Executed)
                .Append(",\"risen\":").Append(b.Stats.Risen).Append(",\"drained\":").Append(b.Stats.Drained)
                .Append(",\"perTurn\":[").Append(string.Join(",", turns)).Append("]}");
            File.WriteAllText(path, json.ToString());
            return path;
        }
    }
}
