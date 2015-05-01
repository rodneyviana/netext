/*=========================================================================================================
  Copyright (c) 2013-2015 Rodney Viana
  http://netext.codeplex.com/

  Distributed under GNU General Public License version 2 (GPLv2) (http://www.gnu.org/licenses/gpl-2.0.html)
============================================================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BuildHelp
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("Help builder for netext");
            Console.WriteLine("--------------------------");

            if (args.Length < 2)
            {
                ShowHelp();
                return 1;
            }
            string sourceFile = args[0];
            string destFile = args[1];
            string[] sourceStr;
            string[] destStr;

            if (!File.Exists(sourceFile))
            {
                Console.WriteLine("Source-File: \"{0}\" not found");
                return 2;
            }
            FileInfo fi = new FileInfo(sourceFile);
            string firstLine = String.Format("// Source Date: {0} {1}", fi.LastWriteTime.ToLongDateString(),
                fi.LastWriteTime.ToLongTimeString());
            if (File.Exists(destFile))
            {
                destStr = File.ReadAllLines(destFile);
                if (destStr.Length > 0)
                {
                    if (destStr[0] == firstLine)
                    {
                        Console.WriteLine("File is up-to-date. No action was performed");
                        return 0;
                    }
                }
            }
            sourceStr = File.ReadAllLines(sourceFile);
            List<string> genFile = new List<string>();
            genFile.Add(firstLine);

            genFile.Add(String.Format("// Source File: {0}", fi.FullName));
            genFile.Add("// This file was generated. Do not modify. Modify Source File instead");
            genFile.Add("#include \"netext.h\"");
            genFile.Add("");
            genFile.Add(@"
EXT_COMMAND(whelp,
            ""Provide hyper-text help"",
            ""{;x,o;;keyword;keyword}"")
{");
            genFile.Add(@"
    string keyword;
    if(HasUnnamedArg(0)) keyword.assign(GetUnnamedArgStr(0));
    // trim string
    std::remove(keyword.begin(), keyword.end(), ' ');

");
            /*
            string strRegex = @"\|\|(\S+)\|\|";
            RegexOptions myRegexOptions = RegexOptions.None;
            Regex myRegex = new Regex(strRegex, myRegexOptions);
            string strTargetString = @"<b>Differences from !||wfrom||</b>\n - Type <<!windex -enumtypes>> or <<!windex -tree>> to enumerate heap objects";
            string strReplace = @"<link cmd=\""!whelp $1\"">$1</link>";

            return myRegex.Replace(strTargetString, strReplace);

             * string strRegex = @"<<(![^>]+)>>";
            RegexOptions myRegexOptions = RegexOptions.None;
            Regex myRegex = new Regex(strRegex, myRegexOptions);
            string strTargetString = @"<b>Differences from !||wfrom||</b>\n - Type <<!windex -enumtypes>> or <<!windex -tree>> to enumerate heap objects";
            string strReplace = @"<link cmd=\""$1\"">$1</link>";

            return myRegex.Replace(strTargetString, strReplace);
             */
            // \|\|(\S+)\|\|
            // <link cmd=\"!whelp $1\">$1</link>
            State curState = State.None;
            int i = 0;
            const string syntError = "Syntax Error at line {0}:\n{1}";
            foreach (string str in sourceStr)
            {
                i++;
                switch (curState)
                {
                    case State.None:
                        // ## is expected
                        if (str.Length < 2 || str.Substring(0, 2) != "##")
                        {
                            Console.WriteLine(syntError, i, str);
                            return 3;
                        }
                        string keyword = str.Substring(2);
                        genFile.Add(
                            String.Format("\tif(keyword==\"{0}\")", keyword)
                            );
                        genFile.Add("\t{");
                        curState = State.Keyword;
                        break;
                    case State.Keyword:
                        if (str.Length > 2 && str.Substring(0, 2) == "##")
                        {
                            genFile.Add("\treturn;");
                            genFile.Add("\t}");
                            curState = State.None;
                            goto case State.None;
                        }
                        genFile.Add(
                            String.Format("\t\tDml(\"{0}\\n\");", Transform(str))
                            );
                        break;
                }
            }
            if (curState == State.Keyword)
            {
                genFile.Add("\treturn;");
                genFile.Add("\t}");
            }
            genFile.Add("\tOut(\"Keyword not found: %s\\n\", keyword.c_str());");
            genFile.Add("}");
            string tmpFile = Path.Combine(fi.DirectoryName, fi.Name + DateTime.Now.Ticks.ToString() + ".tmp");
            File.WriteAllLines(tmpFile, genFile);
            Console.WriteLine("Temporary file '{0}' was created..", tmpFile);
            if (File.Exists(destFile))
            {
                FileInfo fid = new FileInfo(destFile);
                string bakName = fid.FullName + "." + String.Format("{0:x}", DateTime.Now.Ticks) + ".bak";
                fid.MoveTo(bakName);
                Console.WriteLine("Backup created as '{0}'", bakName);
            }
            File.Move(tmpFile, destFile);
            Console.WriteLine("C++ file was created as '{0}'", destFile);

            return 0;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\tBuildHelp <Source-File> <Destination-File>");
            Console.WriteLine("Where:");
            Console.WriteLine("\t<Source-File> is the source file path");
            Console.WriteLine("\t<Destination-File> is the destination file");
        }

        private static string Transform(string Line)
        {
            string tmpStr = Line.Replace("\\", "\\\\");
            tmpStr = tmpStr.Replace("\"", "\\\"");
            Regex regHelp = new Regex(@"\|\|(\S+)\|\|");
            Regex regCmd = new Regex(@"<<([^>]+)>>");
            tmpStr = regHelp.Replace(tmpStr, @"<link cmd=\""!whelp $1\"">$1</link>");
            tmpStr = regCmd.Replace(tmpStr, @"<link cmd=\""$1\"">$1</link>");
            return tmpStr;
        }
    }

    internal enum State
    {
        None,
        Keyword
    }
}