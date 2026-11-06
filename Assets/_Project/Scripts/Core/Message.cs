namespace VampireSpawn.Core
{
    // A sentence the view renders in the player's language: a string-table key plus values, where a Message value is a nested term.
    public readonly struct Message
    {
        public readonly string Key;
        readonly object[] args;
        public object[] Args => args ?? System.Array.Empty<object>();

        public Message(string key, params object[] args) { Key = key; this.args = args; }

        public bool IsEmpty => Key == null;

        public override string ToString() => Args.Length == 0 ? Key : Key + "(" + string.Join(",", Args) + ")";
    }
}
