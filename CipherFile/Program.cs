using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace Crypt
{
    class Program
    {
        private static cryptography _cryptography;
        private static string version = System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion;

        static void displayError(string s)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(String.Format("Crypt v{0} - Built {1} {2}", version, util.RetrieveLinkerTimestamp().ToShortDateString(), util.RetrieveLinkerTimestamp().ToShortTimeString()));
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Error.Write("Error: ");
            Console.ResetColor();
            Console.Error.WriteLine(s);
            Console.Error.WriteLine("Type crypt --help for more information.");
        }

        static void displayHelp()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(String.Format("Crypt v{0} - Built {1} {2}", version, util.RetrieveLinkerTimestamp().ToShortDateString(), util.RetrieveLinkerTimestamp().ToShortTimeString()));
            Console.ResetColor();
            Console.WriteLine("Encrypts and Decrypts files using symmetric encryption.");
            Console.WriteLine("\nCRYPT [-E filename [-I] [-O filename]] [-D filename] [-S filename] [-P password]");
            Console.WriteLine("\n -E\t\tEncrypts a file");
            Console.WriteLine(" -I\t\tUse Random.ORG to generate initalisation vectors");
            Console.WriteLine(" -O\t\tSelect and output filename");
            Console.WriteLine("\n -D\t\tDecrypts a file");
            Console.WriteLine("\n -S\t\tSpecifies a separate Initalization Vector file");
            Console.WriteLine("\n -P\t\tSet the password for the cryptographic process (not recommended)");
        }

        private static List<string> split(string stringToSplit, params char[] delimiters)
        {
            List<string> results = new List<string>();

            bool inQuote = false;
            StringBuilder currentToken = new StringBuilder();
            for (int index = 0; index < stringToSplit.Length; ++index)
            {
                char currentCharacter = stringToSplit[index];
                if (currentCharacter == '"')
                {
                    // When we see a ", we need to decide whether we are
                    // at the start or send of a quoted section...
                    inQuote = !inQuote;
                }
                else if (delimiters.Contains(currentCharacter) && inQuote == false)
                {
                    // We've come to the end of a token, so we find the token,
                    // trim it and add it to the collection of results...
                    string result = currentToken.ToString().Trim();
                    if (result != "") results.Add(result);

                    // We start a new token...
                    currentToken = new StringBuilder();
                }
                else
                {
                    // We've got a 'normal' character, so we add it to
                    // the curent token...
                    currentToken.Append(currentCharacter);
                }
            }

            // We've come to the end of the string, so we add the last token...
            string lastResult = currentToken.ToString().Trim();
            if (lastResult != "") results.Add(lastResult);

            return results;
        }

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string decFilename=null,outFilename=null,password=null, ivFilename=null;
                List<string> inFilenames=new List<string>();
                bool encrypt = false, decrypt=false, internetSource=false;
                _cryptography = new cryptography(true);

                Console.Clear();
                Console.CancelKeyPress += new ConsoleCancelEventHandler(cancelHandler);
                util.writeFullWidth(String.Format("Crypt v{0} - Built {1} {2}", version, util.RetrieveLinkerTimestamp().ToShortDateString(), util.RetrieveLinkerTimestamp().ToShortTimeString()), ConsoleColor.White, ConsoleColor.DarkBlue);

                for (int i = 0; i < args.Length; i++)
                    switch (args[i].ToUpper())
                    {
                        case "-E":
                        case "--ENCRYPT":
                            if (decrypt)
                            {
                                displayError("Syntax Error");
                                return;
                            }
                            string inFileList="";
                            for(int j=i+1;j<args.Length;j++)
                            {
                                if(!args[j].StartsWith("-"))
                                    inFileList+=args[j]+" ";
                                else
                                    break;
                            }
                            if (args[i + 1].Substring(0, 1) != "-")
                            {
                                var result = split(inFileList, ' ');
                                for (int j = 0; j < result.Count(); j++)
                                {
                                    inFilenames.AddRange(Directory.GetFiles(Environment.CurrentDirectory,result[j]));
                                }
                                encrypt = true;
                            }
                            else
                            {
                                displayError("Syntax Error");
                                return;
                            }
                            break;
                        case "-I":
                        case "--INTERNET":
                            if (encrypt)
                                internetSource = true;
                            else
                            {
                                displayError("Syntax Error");
                                return;
                            }
                            break;
                        case "-O":
                        case "--OUTPUT":
                            if (args[i + 1].Substring(0, 1) != "-")
                                if (encrypt)
                                {
                                    outFilename = args[i + 1];
                                    break;
                                }
                            displayError("Syntax Error");
                            return;
                        case "-D":
                        case "--DECRYPT":
                            if (encrypt)
                            {
                                displayError("Syntax Error");
                                return;
                            }
                            if (args[i + 1].Substring(0, 1) != "-")
                            {
                                decFilename = args[i + 1];
                                decrypt = true;
                            }
                            else
                            {
                                displayError("Syntax Error");
                                return;
                            }
                            break;
                        case "-S":
                        case "--SEPARATE":
                            if (args[i + 1].Substring(0, 1) != "-")
                                ivFilename = args[i + 1];
                            else
                            {
                                displayError("Syntax Error");
                                return;
                            }
                            break;
                        case "-P":
                        case "--PASSWORD":
                            if (args[i + 1].Substring(0, 1) != "-")
                            {
                                password = args[i + 1];
                            }
                            else
                            {
                                displayError("Syntax Error");
                                return;
                            }
                            break;
                        case "--HELP":
                        case "-H":
                            displayHelp();
                            return;
                    }

                foreach(string inFilename in inFilenames)
                    if (!File.Exists(inFilename))
                    {
                        displayError("Cannot find file: " + inFilename);
                        return;
                    }


                if (password == null)
                {
                    string[] pass = { "", "" };
                    ConsoleKeyInfo key;

                    for (int i = 0; i < 2; i++)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("Enter your password"+(i==1?" again:":":"));
                        Console.ForegroundColor = ConsoleColor.Gray;
                        do
                        {
                            key = Console.ReadKey(true);

                            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                            {
                                pass[i] += key.KeyChar;
                                Console.Write("*");
                            }
                            else
                            {
                                if (key.Key == ConsoleKey.Backspace && pass[i].Length > 0)
                                {
                                    pass[i] = pass[i].Substring(0, (pass.Length - 1));
                                    Console.Write("\b \b");
                                }
                            }
                        }
                        // Stops Receving Keys Once Enter is Pressed
                        while (key.Key != ConsoleKey.Enter);
                        Console.Write("\n");
                    }

                    if (pass[0] != pass[1])
                    {
                        displayError("Passwords do not match!");
                        return;
                    }
                    else
                        password = pass[0];
                }

                try
                {
                    if (encrypt)
                        _cryptography.encrypt(inFilenames.ToArray(), password,internetSource, outFilename, ivFilename);
                    else if (decrypt)
                        _cryptography.decrypt(decFilename, password, ivFilename);
                    Console.CursorVisible = true;
                }
                catch (Exception e)
                {
                    Console.CursorVisible = true;
                    displayError(e.Message);
                    return;
                }
            }
            else
            {
                displayHelp();
            }
        }
        
        protected static void cancelHandler(object sender, ConsoleCancelEventArgs args)
        {
            _cryptography.bail = true;
            System.Threading.Thread.Sleep(250);
            string[] files = System.IO.Directory.GetFiles(Path.GetTempPath(), "*.dcodetmp", System.IO.SearchOption.TopDirectoryOnly);
            foreach (string file in files)
                File.Delete(file);

            Console.CursorVisible = true;
            Console.ResetColor();
            Console.WriteLine("\nProcess Terminated.");

        }
        
    }
}
