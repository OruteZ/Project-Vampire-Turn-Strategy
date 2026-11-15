// Unity eval script: dumps the data in use (rules, units, upgrades, minions, stages) for the headless player.
// unity command --project-path <tree> script eval --file tools/stage/export.cs
var sb = new System.Text.StringBuilder();
System.Action<object> dump = null;
dump = o =>
{
    if (o == null) { sb.Append("null"); return; }
    var t = o.GetType();
    if (o is string s) { sb.Append('"').Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); return; }
    if (o is bool b) { sb.Append(b ? "true" : "false"); return; }
    if (o is int || o is float) { sb.Append(System.Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture)); return; }
    if (t.IsEnum) { sb.Append((int)o); return; }
    if (o is System.Collections.IEnumerable en)
    {
        sb.Append('['); var first = true;
        foreach (var x in en) { if (!first) sb.Append(','); first = false; dump(x); }
        sb.Append(']'); return;
    }
    sb.Append('{'); var f1 = true;
    foreach (var f in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
    {
        if (!f1) sb.Append(','); f1 = false;
        sb.Append('"').Append(f.Name).Append("\":"); dump(f.GetValue(o));
    }
    sb.Append('}');
};
VampireSpawn.Core.GameData.Use(null);
dump(VampireSpawn.Core.GameData.Current);
var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "tools/stage/data.json");
System.IO.File.WriteAllText(path, sb.ToString());
return path + " " + sb.Length;
