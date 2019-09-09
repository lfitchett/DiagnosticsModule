using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticsCli
{
    public class CommandLineParser
    {
        private Dictionary<string, Tuple<Func<Task>, Dictionary<string, Action<string>>>> commands = new Dictionary<string, Tuple<Func<Task>, Dictionary<string, Action<string>>>>();

        public void RegisterCommand(string command, Func<Task> action, Dictionary<string, Action<string>> args = null)
        {
            commands.Add(command, Tuple.Create(action, args));
        }

        public async Task ParseCommand(string rawInput)
        {
            var iter = rawInput.Split(" ").GetEnumerator();
            if (!iter.MoveNext())
            {
                return;
            }

            /* Parse Command */
            Func<Task> action;
            Dictionary<string, Action<string>> args;
            if (commands.TryGetValue((string)iter.Current, out var temp))
            {
                action = temp.Item1;
                args = temp.Item2;
            }
            else
            {
                Console.WriteLine("Command not recognized");
                return;
            }

            /* Parse args */
            if (args != null)
            {
                while (iter.MoveNext())
                {
                    /* See if flag exists */
                    if (args.TryGetValue((string)iter.Current, out var setArg))
                    {
                        if (!iter.MoveNext())
                        {
                            Console.WriteLine($"Flag {iter.Current} needs an argument");
                            return;
                        }

                        Func<bool> iterHasQuote = () => ((string)iter.Current).Contains('"');
                        string argValue;
                        if (!iterHasQuote())
                        {
                            argValue = (string)iter.Current;
                        }
                        else
                        {
                            /* if arg value is in quotes, argValue goes until the end quote */
                            argValue = ((string)iter.Current).Remove(0, 1) + ' ';
                            while (iter.MoveNext() && !iterHasQuote())
                            {
                                argValue += ((string)iter.Current) + ' ';
                            }
                            argValue += ((string)iter.Current).Remove(((string)iter.Current).Length - 1);
                        }

                        /* call flag function with argValue */
                        setArg(argValue);
                    }
                    else
                    {
                        Console.WriteLine($"Flag {iter.Current} not recognized");
                    }
                }
            }

            /* Execute given command */
            await action();
        }
    }
}
