namespace VampireSpawn.Core
{
    public static class Korean
    {
        public static string Josa(string word, string withFinal, string withoutFinal)
        {
            if (word.Length == 0) return word + withoutFinal;
            var ch = word[word.Length - 1];
            if (ch < 0xAC00 || ch > 0xD7A3) return word + withFinal;
            return word + ((ch - 0xAC00) % 28 != 0 ? withFinal : withoutFinal);
        }
    }
}
