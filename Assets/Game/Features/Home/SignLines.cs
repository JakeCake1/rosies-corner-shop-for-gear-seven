using System;

namespace Game.Features.Home
{
    /// <summary>
    /// Lays the shop name out on the sign in two lines ("Rosie's" / "Corner Shop").
    /// </summary>
    internal static class SignLines
    {
        /// <summary>
        /// Breaks the name at the space that makes the longer line shortest; a name without spaces stays on one line.
        /// </summary>
        /// <param name="name">Shop name as the player sees it.</param>
        /// <returns>The name with at most one line break.</returns>
        public static string Split(string name)
        {
            var text = name.Trim();
            var best = -1;
            var bestLength = text.Length;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != ' ')
                {
                    continue;
                }

                var longer = Math.Max(i, text.Length - i - 1);
                if (longer < bestLength)
                {
                    best = i;
                    bestLength = longer;
                }
            }

            return best < 0 ? text : text.Substring(0, best).TrimEnd() + "\n" + text.Substring(best + 1).TrimStart();
        }

        /// <summary>
        /// Length of the longest line, for sizing the sign font.
        /// </summary>
        /// <param name="lines">Text from <see cref="Split"/>.</param>
        /// <returns>Characters in the longest line.</returns>
        public static int LongestLine(string lines)
        {
            var longest = 0;
            foreach (var line in lines.Split('\n'))
            {
                longest = Math.Max(longest, line.Length);
            }

            return longest;
        }
    }
}
