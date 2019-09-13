using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticsCli
{
    public class CommandLineParser
    {
        private Dictionary<string, Tuple<Func<Task>, Dictionary<string, Action<string>>>> commands = new Dictionary<string, Tuple<Func<Task>, Dictionary<string, Action<string>>>>();

        public void RegisterCommand(string command, Func<Task> action, Dictionary<string, Action<string>> flags = null)
        {
            commands.Add(command, Tuple.Create(action, flags));
        }

        public async Task ParseCommand(string rawInput)
        {
            List<string> args = SplitInput(rawInput);

            string command = args.FirstOrDefault();
            if (command == null || !commands.ContainsKey(command))
            {
                Console.WriteLine($"Command \"{command}\" not recognized");
                return;
            }

            (Func<Task> action, Dictionary<string, Action<string>> flags) = commands[command];

            int argNum = 0;
            var iter = args.Skip(1).GetEnumerator();
            while (iter.MoveNext())
            {
                string flag;
                if (iter.Current.StartsWith('-'))
                {
                    flag = iter.Current;
                    if (!iter.MoveNext())
                    {
                        Console.WriteLine($"Flag {flag} had no argument.");
                        break;
                    }
                }
                else
                {
                    flag = $"{argNum++}";
                }

                if(flags.TryGetValue(flag, out var applyFlag))
                {
                    applyFlag(iter.Current);
                }
                else
                {
                    Console.WriteLine($"No action for arg {flag} : {iter.Current}. Ignoring");
                }
            }

            await action();
        }

        private List<string> SplitInput(string rawInput)
        {
            List<string> args = new List<string>();
            StringBuilder currArg = new StringBuilder();
            bool isInsideQuote = false;
            foreach (char ch in rawInput)
            {
                if (!isInsideQuote && ch == ' ')
                {
                    if (currArg.Length != 0)
                    {
                        args.Add(currArg.ToString());
                        currArg.Clear();
                    }
                    continue;
                }

                if (ch == '"')
                {
                    if (isInsideQuote)
                    {
                        args.Add(currArg.ToString());
                        currArg.Clear();
                        isInsideQuote = false;
                        continue;
                    }

                    isInsideQuote = true;
                    continue;
                }

                currArg.Append(ch);
            }
            if (currArg.Length > 0)
            {
                args.Add(currArg.ToString());
            }

            return args;
        }
    }
}
