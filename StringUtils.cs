using System.Text;

namespace LibStringNaming
{
    public static class StringUtils
    {
        public static string[] SplitWords(string str)
        {
            StringBuilder sb = new StringBuilder();
            List<string> words = new List<string>();

            bool upper = false;

            foreach (var c in str)
            {
                if (char.IsLower(c) && upper && sb.Length > 1)
                {
                    // record and remove the last char
                    char lastChar = sb[sb.Length - 1];
                    sb.Remove(sb.Length - 1, 1);

                    // append word
                    words.Add(sb.ToString());

                    // clear and add the last char
                    sb.Clear();
                    sb.Append(lastChar);

                    // add the current char
                    sb.Append(c);

                    // flags
                    upper = false;
                }
                else if (char.IsUpper(c) && !upper && sb.Length > 0)
                {
                    // add word
                    words.Add(sb.ToString());

                    // clear and add the current char
                    sb.Clear();
                    sb.Append(c);

                    // flags
                    upper = true;
                }
                else if (c == '_' || char.IsWhiteSpace(c))
                {
                    // skip if length is zero
                    if (sb.Length == 0)
                        continue;

                    // add word and clear
                    words.Add(sb.ToString());
                    sb.Clear();

                    // flags
                    upper = false;
                }
                else
                {
                    // add char, and set flags
                    sb.Append(c);
                    upper = char.IsUpper(c);
                }
            }

            if (sb.Length > 0)
                words.Add(sb.ToString());

            return words.ToArray();
        }

        public static string ToPascal(string str)
        {
            string[] words = SplitWords(str);

            StringBuilder sb = new StringBuilder();
            foreach (var word in words)
            {
                sb.Append(char.ToUpper(word[0]));

                for (int i = 1; i < word.Length; i++)
                    sb.Append(word[i]);
            }

            return sb.ToString();
        }
    }
}