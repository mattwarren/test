using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp1
{
    public static class PasteTest
    {
        static void Main()
        {
            Console.WriteLine(Paste(new String[] { "D ouble" + BACKSLASH }, false));
        }

        /// <summary>
        /// Repastes a set of arguments into a linear string that parses back into the originals under pre- or post-2008 VC parsing rules.
        /// The rules for parsing the executable name (argv[0]) are special, so you must indicate whether the first argument actually is argv[0].
        /// </summary>
        public static string Paste(this IEnumerable<string> arguments, bool pasteFirstArgumentUsingArgV0Rules)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string argument in arguments)
            {
                if (pasteFirstArgumentUsingArgV0Rules)
                {
                    pasteFirstArgumentUsingArgV0Rules = false;

                    // Special rules for argv[0]
                    //   - Backslash is a normal character.
                    //   - Quotes used to include whitespace characters.
                    //   - Parsing ends at first whitespace outside quoted region.
                    //   - No way to get a literal quote past the parser.

                    bool hasWhitespace = false;
                    foreach (char c in argument)
                    {
                        if (c == QUOTE)
                            throw new ApplicationException("The argv[0] argument cannot include a double quote.");
                        if (c.IsWhiteSpace())
                            hasWhitespace = true;
                    }
                    if (argument.Length == 0 || hasWhitespace)
                    {
                        sb.Append(QUOTE);
                        sb.Append(argument);
                        sb.Append(QUOTE);
                    }
                    else
                    {
                        sb.Append(argument);
                    }
                }
                else
                {
                    if (sb.Length != 0)
                        sb.Append(' ');

                    // Parsing rules for non-argv[0] arguments:
                    //   - Backslash is a normal character except followed by a quote.
                    //   - 2N backslashes followed by a quote ==> N literal backslashes followed by unescaped quote
                    //   - 2N+1 backslashes followed by a quote ==> N literal backslashes followed by a literal quote
                    //   - Parsing stops at first whitespace outside of quoted region.
                    //   - (post 2008 rule): A closing quote followed by another quote ==> literal quote, and parsing remains in quoting mode.
                    if (argument.Length != 0 && !argument.Any(c => c.IsWhiteSpace() || c == QUOTE))
                    {
                        // Simple case - no quoting or changes needed.
                        sb.Append(argument);
                    }
                    else
                    {
                        sb.Append(QUOTE);
                        int idx = 0;
                        while (idx < argument.Length)
                        {
                            char c = argument[idx++];
                            if (c == BACKSLASH)
                            {
                                int numBackSlash = 1;
                                while (idx < argument.Length && argument[idx] == BACKSLASH)
                                {
                                    idx++;
                                    numBackSlash++;
                                }
                                if (idx == argument.Length)
                                {
                                    // We'll emit an end quote after this so must double the number of backslashes.
                                    sb.Append(BACKSLASH, numBackSlash * 2);
                                }
                                else if (argument[idx] == QUOTE)
                                {
                                    // Backslashes will be followed by a quote. Must double the number of backslashes.
                                    sb.Append(BACKSLASH, numBackSlash * 2 + 1);
                                    sb.Append(QUOTE);
                                    idx++;
                                }
                                else
                                {
                                    // Backslash will not be followed by a quote, so emit as normal characters.
                                    sb.Append(BACKSLASH, numBackSlash);
                                }
                                continue;
                            }
                            if (c == QUOTE)
                            {
                                // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
                                // by another quote (which parses differently pre-2008 vs. post-2008.)
                                sb.Append(BACKSLASH);
                                sb.Append(QUOTE);
                                continue;
                            }
                            sb.Append(c);
                        }
                        sb.Append(QUOTE);
                    }
                }
            }

            return sb.ToString();
        }

        private static bool IsWhiteSpace(this char c)
        {
            return Char.IsWhiteSpace(c);
        }

        private const char NUL = (char)0;
        private const char QUOTE = '\"';
        private const char BACKSLASH = '\\';
    }
}
