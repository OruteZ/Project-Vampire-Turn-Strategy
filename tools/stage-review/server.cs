// Stage dressing feedback tool (docs/art/stage-dressing.md 7). Built by build.ps1 with the csc that ships with Windows, so C# 5 only.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

class StageReview
{
    static string Root, Tool, Out;
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
    static readonly object Gate = new object();
    static Process capture;
    static readonly StringBuilder captureLog = new StringBuilder();

    static void Main(string[] args)
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, ".git")) && !System.IO.File.Exists(Path.Combine(dir, ".git"))) dir = Path.GetDirectoryName(dir.TrimEnd('\\'));
        if (dir == null) { Console.WriteLine("저장소를 못 찾았습니다. exe 는 저장소 안 tools/stage-review 에 두세요."); Console.ReadLine(); return; }
        Root = dir;
        Tool = Path.Combine(Root, "tools", "stage-review");
        Out = Path.Combine(Root, "verify-artifacts", "stage-review");
        Directory.CreateDirectory(Path.Combine(Out, "shots"));

        HttpListener http = null;
        string url = null;
        for (var port = 5178; port < 5190 && http == null; port++)
        {
            try
            {
                var l = new HttpListener();
                url = "http://localhost:" + port + "/";
                l.Prefixes.Add(url);
                l.Start();
                http = l;
            }
            catch (HttpListenerException) { }
        }
        if (http == null) { Console.WriteLine("열 수 있는 포트가 없습니다 (5178~5189)."); Console.ReadLine(); return; }
        Console.WriteLine("스테이지 꾸밈 피드백: " + url + "  (이 창을 닫으면 끝납니다)");
        Console.WriteLine("저장 위치: " + Out);
        if (!args.Contains("--no-browser")) Process.Start(url);

        while (true)
        {
            var ctx = http.GetContext();
            System.Threading.ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
        }
    }

    static void Handle(HttpListenerContext ctx)
    {
        var req = ctx.Request; var res = ctx.Response;
        try
        {
            var path = Uri.UnescapeDataString(req.Url.AbsolutePath);
            if (path == "/") Serve(res, Path.Combine(Tool, "index.html"));
            else if (path.StartsWith("/data/")) Serve(res, Safe(Out, path.Substring(6)));
            else if (path == "/api/state") Send(res, State());
            else if (path == "/api/feedback" && req.HttpMethod == "POST") Send(res, SaveFeedback(Body(req)));
            else if (path == "/api/feedback/delete" && req.HttpMethod == "POST") Send(res, DeleteFeedback(Body(req)));
            else if (path == "/api/capture" && req.HttpMethod == "POST") Send(res, StartCapture(Body(req)));
            else { res.StatusCode = 404; res.Close(); }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            try { res.StatusCode = 500; Send(res, new Dictionary<string, object> { { "error", e.Message } }); } catch { }
        }
    }

    static string Safe(string root, string rel)
    {
        var full = Path.GetFullPath(Path.Combine(root, rel.Replace('/', '\\')));
        if (!full.StartsWith(Path.GetFullPath(root) + "\\", StringComparison.OrdinalIgnoreCase)) throw new Exception("bad path");
        return full;
    }

    static Dictionary<string, object> Body(HttpListenerRequest req)
    {
        using (var r = new StreamReader(req.InputStream, Encoding.UTF8)) return Json.Deserialize<Dictionary<string, object>>(r.ReadToEnd());
    }

    static void Serve(HttpListenerResponse res, string path)
    {
        if (!System.IO.File.Exists(path)) { res.StatusCode = 404; res.Close(); return; }
        var ext = Path.GetExtension(path).ToLowerInvariant();
        res.ContentType = ext == ".html" ? "text/html; charset=utf-8" : ext == ".png" ? "image/png" : ext == ".json" ? "application/json; charset=utf-8" : "application/octet-stream";
        res.Headers["Cache-Control"] = "no-store";
        var bytes = System.IO.File.ReadAllBytes(path);
        res.OutputStream.Write(bytes, 0, bytes.Length);
        res.Close();
    }

    static void Send(HttpListenerResponse res, object o)
    {
        var bytes = Encoding.UTF8.GetBytes(Json.Serialize(o));
        res.ContentType = "application/json; charset=utf-8";
        res.OutputStream.Write(bytes, 0, bytes.Length);
        res.Close();
    }

    static object Read(string path) { return Json.DeserializeObject(System.IO.File.ReadAllText(path, Encoding.UTF8)); }

    static object State()
    {
        var stages = new List<object>();
        var metas = Directory.GetFiles(Path.Combine(Out, "shots"), "*.json")
            .OrderBy(f => { int n; return int.TryParse(Path.GetFileNameWithoutExtension(f).TrimStart('s'), out n) ? n : 999; });
        foreach (var m in metas)
        {
            var id = Path.GetFileNameWithoutExtension(m);
            var fbDir = Path.Combine(Out, "feedback", id);
            var hDir = Path.Combine(Out, "history", id);
            stages.Add(new Dictionary<string, object>
            {
                { "meta", Read(m) },
                { "stamp", System.IO.File.GetLastWriteTime(m).ToString("yyyy-MM-dd HH:mm") },
                { "feedback", Directory.Exists(fbDir) ? Directory.GetFiles(fbDir, "*.json").OrderBy(f => f).Select(Read).ToArray() : new object[0] },
                { "history", Directory.Exists(hDir) ? Directory.GetDirectories(hDir).Select(Path.GetFileName).OrderByDescending(s => s).ToArray() : new string[0] },
            });
        }
        lock (Gate)
            return new Dictionary<string, object>
            {
                { "stages", stages },
                { "capture", new Dictionary<string, object> { { "running", capture != null && !capture.HasExited }, { "log", captureLog.ToString() } } },
            };
    }

    static object SaveFeedback(Dictionary<string, object> b)
    {
        var id = (string)b["stage"];
        if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9]+$")) throw new Exception("bad stage");
        var dir = Path.Combine(Out, "feedback", id);
        Directory.CreateDirectory(dir);
        var name = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var png = (string)b["drawing"];
        System.IO.File.WriteAllBytes(Path.Combine(dir, name + ".png"), Convert.FromBase64String(png.Substring(png.IndexOf(',') + 1)));
        var fb = new Dictionary<string, object>
        {
            { "id", name }, { "stage", id }, { "view", b["view"] }, { "text", b["text"] },
            { "kind", b.ContainsKey("kind") && (string)b["kind"] == "규칙" ? "규칙" : "꾸밈" }, { "cells", b.ContainsKey("cells") ? b["cells"] : new object[0] },
            { "image", "feedback/" + id + "/" + name + ".png" }, { "created", DateTime.Now.ToString("yyyy-MM-dd HH:mm") },
            { "status", "대기" }, { "reply", "" },
        };
        System.IO.File.WriteAllText(Path.Combine(dir, name + ".json"), Json.Serialize(fb), new UTF8Encoding(false));
        return fb;
    }

    static object DeleteFeedback(Dictionary<string, object> b)
    {
        var dir = Safe(Out, "feedback/" + b["stage"]);
        var json = Safe(dir, b["id"] + ".json");
        var fb = (Dictionary<string, object>)Read(json);
        if ((string)fb["status"] != "대기") throw new Exception("반영된 피드백은 지우지 않습니다");
        System.IO.File.Delete(json);
        System.IO.File.Delete(Safe(dir, b["id"] + ".png"));
        return fb;
    }

    static object StartCapture(Dictionary<string, object> b)
    {
        lock (Gate)
        {
            if (capture != null && !capture.HasExited) return new Dictionary<string, object> { { "error", "이미 찍는 중" } };
            var ids = b.ContainsKey("ids") ? (string)b["ids"] : "";
            if (!System.Text.RegularExpressions.Regex.IsMatch(ids, "^[a-z0-9,]*$")) throw new Exception("bad ids");
            captureLog.Clear();
            var psi = new ProcessStartInfo(Bash(), "\"" + Path.Combine(Tool, "capture.sh").Replace('\\', '/') + "\" " + ids)
            {
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8, WorkingDirectory = Root,
            };
            capture = new Process { StartInfo = psi, EnableRaisingEvents = true };
            DataReceivedEventHandler log = (s, e) => { if (e.Data != null) lock (Gate) captureLog.AppendLine(e.Data); };
            capture.OutputDataReceived += log;
            capture.ErrorDataReceived += log;
            capture.Start();
            capture.BeginOutputReadLine();
            capture.BeginErrorReadLine();
            return new Dictionary<string, object> { { "running", true } };
        }
    }

    // Git Bash, not the WSL bash.exe that WindowsApps puts on PATH.
    static string Bash()
    {
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var guess = Path.Combine(pf, "Git", "bin", "bash.exe");
        if (System.IO.File.Exists(guess)) return guess;
        foreach (var p in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
        {
            var git = Path.Combine(p.Trim(), "git.exe");
            if (!System.IO.File.Exists(git)) continue;
            var bash = Path.GetFullPath(Path.Combine(p.Trim(), "..", "bin", "bash.exe"));
            if (System.IO.File.Exists(bash)) return bash;
        }
        throw new Exception("Git Bash 를 못 찾았습니다");
    }
}
