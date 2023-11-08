// Kartas Starfurie
// kstarfurie@yahoo.com

#define PRE_PARSE_LOG_ON
#define POST_PARSE_LOG_ON

using System;
using System.Collections.Generic;
using System.Text;
using Advanced_Combat_Tracker;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Globalization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using Microsoft.Win32;
using System.Windows.Forms.VisualStyles;

namespace SBCombatParser
{

    public class SBregExHelperFuncs
    {
        public delegate string SBregExHelper_PreParse(string str);
        public delegate Dictionary<string, string> SBregExHelper_PostParse( Dictionary<string, string> dic );

        public static List<SBregExHelperFuncs.SBregExHelper_PreParse> GlobalPreParseFuncList = new List<SBregExHelperFuncs.SBregExHelper_PreParse>();
        public static List<SBregExHelperFuncs.SBregExHelper_PostParse> GlobalPostParseFuncList = new List<SBregExHelperFuncs.SBregExHelper_PostParse>();

        public static string SBregExHelper_PreParse_CaseReplace(string str)
        {
            string ret = Regex.Replace(str, @" (you)[r]?([\.!])?", @" YOU$2", RegexOptions.IgnoreCase);
            ret = Regex.Replace(ret, @"(Someone)", @"SOMEONE", RegexOptions.IgnoreCase);
            SBregExHelper_PreParse_LogWriter(str, ret);

            return ret;
        }

        public static string SBregExHelper_PreParse_AreIs(string str)
        {
            string ret = Regex.Replace(str, @" (are) ", @" is ");
            
            SBregExHelper_PreParse_LogWriter(str, ret);

            return ret;
        }

        private static void SBregExHelper_PreParse_LogWriter(string original, string after, [CallerMemberName] string callingMethod = "")
        {
            if (!after.Equals(original))
            {
                GlobalVariables.WriteLineToPreParseLog("Before : " + original, callingMethod);
                GlobalVariables.WriteLineToPreParseLog("After  : " + after, callingMethod);
                GlobalVariables.WriteLineToPreParseLog("", callingMethod);
            }
        }

        public static Dictionary<string,string> SBregExHelper_PostParse_Source_ReduceToFirstName(Dictionary<string,string> dict)
        {
            dict["source"] = ReduceToFirstName(dict["source"]);

            return dict;
        }

        public static Dictionary<string, string> SBregExHelper_PostParse_Target_ReduceToFirstName(Dictionary<string, string> dict)
        {
            dict["target"] = ReduceToFirstName(dict["target"]);

            return dict;
        }

        private static string ReduceToFirstName(string str)
        {
            //Name Check and Rename
            //Trying to help the Last name randomness
            //Get rid of "The" in front of names
            string originalStr = str;

            str = Regex.Replace(str, @"The ", @"the ", RegexOptions.IgnoreCase);

            string[] nameSplit = str.Split(' ');

            if (nameSplit.Length > 1)
            {
                if (nameSplit[0].Contains("the"))
                {
                    //GlobalVariables.WriteLineToPostParseLog("Compound Name -- Found :: " + str);
                    //foreach (string s in nameSplit)
                    //{
                    //    GlobalVariables.WriteLineToPostParseLog("Compound Name -- Index :: " + s);
                    //}

                    str = "";
                    for (int i = 1; i < nameSplit.Length; i++)
                    {
                        str = str + nameSplit[i] + " ";
                    }

                    str = str.Trim(' ');
                    //GlobalVariables.WriteLineToPostParseLog("Compound Name -- Modified :: " + str);
                }
            }

            //Exception for the Huntress pet Vashteera's Companion and Vashteera's Thrall
            //Exception for black mantle .. key off Shadow Mantle
            switch (str.Trim(' ', '"'))
            {
                //NPCs, Pets and black mantle
                case "Vorok Mud-Chief":
                case "Mud Ogre":
                case "Moss Ogre":
                case "Tavok Chief-brother":
                case "Novac the Bellugh Nuathal Priest":
                case "Vashteera's Companion":
                case "Vashteera's Thrall":
                case "black mantle":
                    //GlobalVariables.WriteLineToPostParseLog("Compound Name -- No Rename :: " + str);
                    break;

                default:
                    nameSplit = str.Split(' ');
                    if (nameSplit.Length > 1)
                    {
                        //Possible Last name or more
                       // GlobalVariables.WriteLineToPostParseLog("Compound Name -- Found :: " + str);
                        //foreach (string s in nameSplit)
                        //{
                        //    GlobalVariables.WriteLineToPostParseLog("Compound Name -- Index :: " + s);
                        //}
                        str = nameSplit[0];
                        //GlobalVariables.WriteLineToPostParseLog("Compound Name -- Modified :: " + str);
                    }
                    break;
            }

            if (!originalStr.Equals(str))
            {
                GlobalVariables.WriteLineToPostParseLog("Compound Name -- Modified :: " + originalStr + " TO "+ str);
            }

            return str;
        }

    }

    public class SBregExUsage
    {
        public string regExType = null; 
        public string regExSubType = null;
        public string regExString = null;
        public int regExUsageCount = 0;
        private Regex regEx = null;
        public bool logIt = false;
        private string regLogFileName = null;
        private string regParseFileName = null;
        public RegexOptions regExOptions = RegexOptions.None;

        public List<SBregExHelperFuncs.SBregExHelper_PreParse> regPreParseFuncList = null;
        public List<SBregExHelperFuncs.SBregExHelper_PostParse> regPostParseFuncList = null;

        public SBregExUsage(string regExType, string regExSubType, string regExString, RegexOptions regExOptions, bool logIt, string regLogFileName = null)
        {
            this.regExType = regExType;
            this.regExSubType =regExSubType;
            this.regExString =regExString;  
            this.regExOptions =regExOptions;
            this.logIt = logIt;
            if (regLogFileName == null )
            {
                this.regLogFileName = "SB_REG_" + this.regExType.Replace(' ', '_') + "_" + this.regExSubType.Replace(' ', '_')+".log.txt";
                this.regParseFileName = this.regLogFileName.Replace(".log", ".parse");
            } 
            else
            {
                this.regLogFileName = regLogFileName;
                this.regParseFileName = this.regLogFileName.Replace(".", ".parse.");
            }

            this.regEx = new Regex(this.regExString, this.regExOptions);

            this.regPreParseFuncList = new List<SBregExHelperFuncs.SBregExHelper_PreParse>();
            this.regPostParseFuncList = new List<SBregExHelperFuncs.SBregExHelper_PostParse>();

        }

        public void InitLog()
        {
            if(this.logIt)
            {
                DateTime currentDateTime = DateTime.Now;
                GlobalVariables.WriteLineToRegExFile(this.regLogFileName, "Version     :: " + GlobalVariables.version);
                GlobalVariables.WriteLineToRegExFile(this.regLogFileName, "DateTime    :: " + currentDateTime);
                GlobalVariables.WriteLineToRegExFile(this.regLogFileName, "RegExType   :: " + this.regExType + " :: " + this.regExSubType);
                GlobalVariables.WriteLineToRegExFile(this.regLogFileName, "RegExString :: " + this.regExString);
                GlobalVariables.WriteLineToRegExFile(this.regLogFileName, "");
                GlobalVariables.WriteLineToRegExFile(this.regLogFileName, "");


                GlobalVariables.WriteLineToRegExFile(this.regParseFileName, "Version     :: " + GlobalVariables.version);
                GlobalVariables.WriteLineToRegExFile(this.regParseFileName, "DateTime    :: " + currentDateTime);
                GlobalVariables.WriteLineToRegExFile(this.regParseFileName, "RegExType   :: " + this.regExType + " :: " + this.regExSubType);
                GlobalVariables.WriteLineToRegExFile(this.regParseFileName, "RegExString :: " + this.regExString);
                GlobalVariables.WriteLineToRegExFile(this.regParseFileName, "");
                GlobalVariables.WriteLineToRegExFile(this.regParseFileName, $"{"Time",-8} : {"Value_Type",-10} : {"Source",-25} : {"Target",-25} : {"Ability",-25} : {"Event_type",-12} : {"Event_detail",-12} : {"Value", -6}");
            }
        }

        public Dictionary<string, string> Matches(string line)
        {
            Dictionary<string,string> result = new Dictionary<string,string>();

            foreach( var preProcessFunc in this.regPreParseFuncList )
            {
                line = preProcessFunc(line);
            }

            MatchCollection match = this.regEx.Matches(line);
            if (match != null && match.Count > 0) 
            {
                this.regExUsageCount++;
                GroupCollection g = match[0].Groups;

                result["time"] = g["time"].Value.Trim(' ', '"');
                result["type"] = g["type"].Value.Trim(' ', '"');
                result["source"] = g["source"].Value.Trim(' ', '"');
                result["target"] = g["target"].Value.Trim(' ', '"');
                result["ability"] = g["ability"].Value.Trim(' ', '"');
                result["event_type"] = g["event_type"].Value.Trim(' ', '"');
                result["event_detail"] = g["event_detail"].Value.Trim(' ', '"');
                result["value"] = g["value"].Value.Trim(' ', '"');

                if (this.logIt)
                {
                    GlobalVariables.WriteLineToRegExFile(this.regLogFileName, line);
                    GlobalVariables.WriteLineToRegExFile(this.regParseFileName, $"{result["time"],-8} : {result["type"],-10} : {result["source"],-25} : {result["target"],-25} : {result["ability"],-25} : {result["event_type"],-12} : {result["event_detail"],-12} : {result["value"],-6}");
                }

                foreach (var postProcessFunc in this.regPostParseFuncList)
                {
                    result = postProcessFunc(result);
                }

            }

            return result;
        }
    }

    public class GlobalVariables
    {
        static public string SBLogDir = "./SB_Logs";
        static public string logFile = "SBCombatParser.log.txt";
        static public string noParseFile = "SBMissParse.log.txt";
        static public string unkFile = "SBUnkParse.log.txt";
        static public string unkPBFile = "SBUnkPB.log.txt";
        static public string preParseFile = "SBPreParse.log.txt";
        static public string postParseFile = "SBPostParse.log.txt";

        static public string version = "0.0.12.1";
        static public readonly object fileLock = new object();
        static public readonly object missFilelock = new object();
        static public readonly object unkFilelock = new object();
        static public readonly object unkPBFilelock = new object();
        static public readonly object regExFileWriteLock = new object();
        static public readonly object preParseWriteLock = new object();
        static public readonly object postParseWriteLock = new object();

        static public SBSetupHelper setupHelper = new SBSetupHelper();

        static public void WriteLineToDebugLog(string line, [CallerMemberName] string callingMethod = "")
        {
#if DEBUG
            lock(fileLock)
            {
                using (StreamWriter writer = File.AppendText(SBLogDir+"/"+logFile))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(callingMethod + " :: " + line);
                }    
            }
#endif
        }
        static public void WriteLineToPreParseLog(string line, [CallerMemberName] string callingMethod = "")
        {
#if DEBUG
#if PRE_PARSE_LOG_ON
            lock (preParseWriteLock)
            {
                using (StreamWriter writer = File.AppendText(SBLogDir + "/" + preParseFile))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(callingMethod + " :: " + line);
                }
            }
#endif
#endif
        }

        static public void WriteLineToPostParseLog(string line, [CallerMemberName] string callingMethod = "")
        {
#if DEBUG
#if POST_PARSE_LOG_ON
            lock (postParseWriteLock)
            {
                using (StreamWriter writer = File.AppendText(SBLogDir + "/" + postParseFile))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(callingMethod + " :: " + line);
                }
            }
#endif
#endif
        }


        static public void WriteLineToRegExFile(string filename, string line)
        {
#if DEBUG
            lock (regExFileWriteLock)
            {
                using (StreamWriter writer = File.AppendText(SBLogDir + "/" + filename))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(line);
                }
            }
#endif
        }


        static public void WriteLineToMissedParse(string line)
        {
#if DEBUG
            lock (missFilelock)
            {
                using (StreamWriter writer = File.AppendText(SBLogDir + "/" + noParseFile))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(line);
                }
            }
#endif
        }

        static public void WriteLineToUnknownParse(string line)
        {
#if DEBUG
            lock (unkFilelock)
            {
                using (StreamWriter writer = File.AppendText(SBLogDir + "/" + unkFile))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(line);
                }
            }
#endif
        }

        static public void WriteLineToUnknownPBLog(string line)
        {
            lock (unkPBFilelock)
            {
                using (StreamWriter writer = File.AppendText(SBLogDir + "/" + unkPBFile))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(line);
                }
            }
        }
        public class SBSetupHelper
        {
            // Regex
            //public static List<String> regExDesc;
            //public static List<int> regExUse;
            //public static List<List<Regex>> allRegEx;
            //public static List<Regex> damageLines;
            //public static List<Regex> healLines;
            //public static List<Regex> evadedLines;
            //public static List<Regex> diedLines;
            //public static List<Regex> buffLines;
            //public static List<Regex> buffStopLines;
            //public static List<Regex> oddLines;
            //public static List<Regex> clcEvents;

            public static List<List<SBregExUsage>> allRegEx;
            public static List<SBregExUsage> highPriorityRegEx;
            public static List<SBregExUsage> normalPriorityRegEx;
            public static List<SBregExUsage> lowPriorityRegEx;
            public static List<SBregExUsage> zeroPriorityRegEx;

            public SBSetupHelper()
            {
                

                //regExDesc = new List<String>();
                //regExUse = new List<int>();
                //allRegEx = new List<List<Regex>>();
                //damageLines = new List<Regex>();
                //healLines = new List<Regex>();
                //evadedLines = new List<Regex>();
                //diedLines = new List<Regex>();
                //buffLines = new List<Regex>();
                //buffStopLines = new List<Regex>();
                //oddLines = new List<Regex>();
                //clcEvents = new List<Regex>();

                allRegEx = new List<List<SBregExUsage>>(); 
                highPriorityRegEx = new List<SBregExUsage>();
                normalPriorityRegEx = new List<SBregExUsage>();
                lowPriorityRegEx = new List<SBregExUsage>();
                zeroPriorityRegEx = new List<SBregExUsage>();

                //allRegEx.Add(clcEvents);
                //allRegEx.Add(healLines);
                //allRegEx.Add(damageLines);
                //allRegEx.Add(evadedLines);
                //allRegEx.Add(diedLines);
                //allRegEx.Add(buffLines);
                //allRegEx.Add(oddLines);
                //allRegEx.Add(buffStopLines);

                allRegEx.Add(highPriorityRegEx); 
                allRegEx.Add(normalPriorityRegEx);
                allRegEx.Add(lowPriorityRegEx);
                allRegEx.Add(zeroPriorityRegEx);

                String myRegEx = @"";
                
                SBregExUsage sbreu = null;

                bool writeALLlogFiles = true;

                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>CLC) (?<type>.*) (?<event_type>.*) \:\: (?<event_detail>.*)";
                highPriorityRegEx.Add(new SBregExUsage("CLC", "Event", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

                //myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*)[r']?[s]? (?<ability>.*'+.*) (?<type>heal)s (?<target>.*) for (?<value>\d*) points\.";
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.+)[r']+[s]? (?<ability>.+'+.+) (?<type>heal)s (?<target>.*) for (?<value>\d*) points[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Heal","Apos",myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
               
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']?[s]? (?<ability>.*) (?<type>heal)s (?<target>.*?)'?s? (?<event_type>stamina) for (?<value>\d*) points[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Heal", "Stamina", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
               
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']?[s]? (?<ability>.*) (?<type>heal)s (?<target>.*) for (?<value>\d*) points[\.!]";
                sbreu = new SBregExUsage("Heal", "CatchAll", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']?[s]? (?<ability>Vamp.*'+.*) (?<type>.*)s (?<value>\d*) points of [helthman]* from (?<target>.*)[\.!]";
                //normalPriorityRegEx.Add(new SBregExUsage("Drain", "VampKiss", myRegEx, RegexOptions.Compiled, true));

                //myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*)[r']?[s]? (?<ability>Needs.*One) (?<type>.*)s (?<value>\d*) points of [helthman]* from (?<target>.*)[\.!]";
                //normalPriorityRegEx.Add(new SBregExUsage("Drain", "Needs Of The One", myRegEx, RegexOptions.Compiled, true));

                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*) (?<type>drain)s (?<value>\d*) points of (?<event_type>[helthman]*) from (?<target>.*) with (?<ability>.*)[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Drain", "Val First", myRegEx, RegexOptions.Compiled, writeALLlogFiles | true));

                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']?[s]? (?<ability>.*) (?<type>.*)s (?<value>\d*) points of (?<event_type>[helthman]*) from (?<target>.*)[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Drain", "Val Last", myRegEx, RegexOptions.Compiled, writeALLlogFiles | true));

                //Backstab drains
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) (?<type>.*?)[e]?[s]? (?<value>\d*) points of (?<event_type>.*?)\W?damage from (?<source>\w*)[r']?[s]? (?<ability>.*)[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Drain", "Backstab", myRegEx, RegexOptions.Compiled, writeALLlogFiles | true));


                //Call the Sky's Fury Damage
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']?[s]? (?<ability>Call the Sky.*) (?<type>.{3,8})s (?<target>.*) for (?<value>\d*) .*[\.!]";
                sbreu = new SBregExUsage("Damage", "Call the Sky's Fury", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //Saint Malorn's Wrath
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>\w*)[r's]* (?<ability>Saint.*'.*) (?<type>.{3,8})s (?<target>.*) for (?<value>\d*) .*[\.!]";
                sbreu = new SBregExUsage("Damage", "Saint Malorn's Wrath", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //Darius' Fist
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']?[s]? (?<ability>Darius' Fist) (?<type>.{3,11}?)s? (?<target>.*) for (?<value>\d*) .*[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Damage", "Darius' Fist", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
                
                //Pallando's Pernicious Puns
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']*[s]* (?<ability>Pallando.*Puns) (?<type>\w{3,11}?) (?<target>.*) for (?<value>\d*) .*[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Damage", "Pallando's Pernicious Puns", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

                //Hedge of Thorns
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']{1}[s]* (?<ability>Hedge.*?s) (?<type>\w{3,11}?)s (?<target>.*) for (?<value>\d*) .*[\.!]";
                sbreu = new SBregExUsage("Damage", "Hedge of Thorns", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                normalPriorityRegEx.Add(sbreu);

                //Grasp of Thorns
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']{1}[s]* (?<ability>Grasp.*) (?<type>\w{3,11}?)s (?<target>.*) for (?<value>\d*) .*[\.!]";
                sbreu = new SBregExUsage("Damage", "Grasp of Thorns", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                normalPriorityRegEx.Add(sbreu);

                //Stamina Damage
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']*[s]* (?<event_type>Stamina) Damage (?<type>hit)[s]? (?<target>.*) for (?<value>\d*) .*[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Damage", "Stamina hit", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
                
                //myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*) (?<type>hit)s the (?<target>.*) for (?<value>\d*) .*\.";
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*)[r']?[s]? (?<type>hit)[s]? (?<target>.*) for (?<value>\d*) .*[\.!]";
                sbreu = new SBregExUsage("Damage", "Melee hit", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);
               
                //target = YOU
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*)[r']?[s]? (?<type>hit)s (?<target>.*) for (?<value>\d*) .*[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Damage", "Hit You", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
               
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*)[r']?[s]? (?<type>take) (?<value>\d*) .* from (?<source>.*)[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Damage", "Spell Hit You", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

                //Matches hurt, shock, smite, heal
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']{1}[s]* (?<ability>.*?) (?<type>\w{3,11}?)s (?<target>.*) for (?<value>\d*) .*[\.!]";
                sbreu = new SBregExUsage("Damage", "Spell", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //Proc Damage - You
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>[The]*) (?<ability>.*) (?<type>.*)s (?<target>.*) for (?<value>\d*) .*[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Damage", "Proc You", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
                
                //Proc Damage
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*)[r']?[s]? (?<type>.*) (?<ability>.*)s (?<target>.*) for (?<value>\d*) .*!";
                normalPriorityRegEx.Add(new SBregExUsage("Damage", "Proc", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
               
                //Bleed Damage
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) takes? (?<value>\d*) .*(?<source>damage) from (?<type>.*)ing[\.!]";
                sbreu = new SBregExUsage("Damage", "Bleed", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //Miss -- Power
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r's]? power (?<type>miss)[es]* (?<target>.*)[\.!]";
                sbreu = new SBregExUsage("Evade", "Miss Power", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //Miss
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*) (?<type>miss)[es]* (?<target>.*)[\.!]";
                sbreu = new SBregExUsage("Evade", "Miss Melee", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //You Block
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) (?<type>block)[s]? (?<source>.*)'s .*[\.!]";
                sbreu = new SBregExUsage("Evade", "Block You", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu); 
                
                //Parry
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) (?<type>parr)[yies]+ (?<source>.*)'s .*[\.!]";
                sbreu = new SBregExUsage("Evade", "Parry", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);
                //All Dodge
                //myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) (?<type>dodge)s? (?<source>.*?)[r's]+ attack[\.!]";
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) (?<type>dodge)s? (?<source>.*?)[r']?[s]? attack[\.!]";
                sbreu = new SBregExUsage("Evade", "Dodge", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //Shadow Mantle
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) [areis]+ (?<type>surround)ed by a (?<source>.*)[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Debuff", "Shadow Mantle", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
               
                //Knavery (Blind)
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*) (?<type>kick[s]? dirt) .* (?<target>\w*)[r's]+ .*[\.!]";
                sbreu = new SBregExUsage("Debuff", "Blind", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //Use Power -- Error use
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*\[(?<event_type>Powers)\] (?<event_detail>.*):(?<source>.*?) (?<type>use)s? [\Wa]?(?<ability>.*)[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Error Use", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
               
                //Use Power -- Error cast
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*\[(?<event_type>Powers)\] (?<event_detail>.*):(?<source>.*?) (?<type>cast)s? [\Wa]?(?<ability>.*)[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Error Cast", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
                
                //Use Power -- enter
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) (?<type>enter)s? [\Wa]?(?<ability>.*)[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Enter", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
               
                //Use Power -- cast
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) (?<type>cast)s? [\Wa]?(?<ability>.*)[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Cast", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
                
                //Use Power -- assume
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) (?<type>assume)s? [\Wa]?(?<ability>.*)[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Assume", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
                
                //Use Power -- use
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) (?<type>use)s? [\Wa]?(?<ability>.*)[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Use", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

                //Use Power -- execute - prepare
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) (?<event_type>prepare) to (?<type>execute)s? [\Wa]*(?<ability>.*)[\.!]";
                sbreu = new SBregExUsage("Power", "Execute-prepare", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                normalPriorityRegEx.Add(sbreu);

                //Use Power -- execute
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) (?<type>execute)s? [\Wa]*(?<ability>.*)[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Execute", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

                //active -- begin / no longer
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) [a]?[r]?[e]?[i]?[s]?\W?(?<event_type>.*?)[s]? (?<type>active)ly (?<ability>.*)ing[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Active", "begin_no_longer", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

                //Use Power -- Cry -- Hold Fast
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) (?<type>cr[yi]?)[es]* \'(?<ability>.*)\'[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Cry_Hold_Fast", myRegEx, RegexOptions.Compiled, writeALLlogFiles | true));


                //has Died
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*\[Combat\] Info\: (?<source>.*) [hasve]* (?<type>die).*!";
                sbreu = new SBregExUsage("Combat", "Info Death", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                lowPriorityRegEx.Add(sbreu);
                
                //Taunt
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*\[Combat\] Info\: (?<target>.*) (?<type>[atck]*)s? (?<source>.*)";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Taunt", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));
                
                

                //REDO They are TOO GENERIC ... its catching EVERYTHING
                //Buff Stops - Fade, returns
                //myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>\w*)+ (?<ability>.*) (?<type>.*)[\.!]+";
                //regExUse.Add(0);
                //regExDesc.Add("Buff Stop - Fades :: " + myRegEx);
                //buffStopLines.Add(new Regex(myRegEx, RegexOptions.Compiled));

                //Buff Stops - Fade, returns
                //myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>\w*)[r's]+ (?<ability>.*) (?<type>.*)[\.!]+";
                //regExUse.Add(0);
                //regExDesc.Add("Buff Stop - Fades Apos :: " + myRegEx);
                //buffStopLines.Add(new Regex(myRegEx, RegexOptions.Compiled));


            }

        }


    }

    public class SBCombatParser : IActPluginV1
    {

        //public SBSetupHelper SetupHelper = null;

        public void DeInitPlugin()
        {
           
            GlobalVariables.WriteLineToDebugLog("DeInitPlugin !!!!");
            
            string myline = $"RegEx Use :: {"Count",-6} :: {"Type",-8} :: {"Sub Type",-30} :: {"RegEx String",-100} ";
            GlobalVariables.WriteLineToDebugLog(myline);

            foreach (List<SBregExUsage> list in GlobalVariables.SBSetupHelper.allRegEx)
            {
                foreach (SBregExUsage s in list)
                {
                    myline = $"RegEx Use :: {s.regExUsageCount,-6} :: {s.regExType,-8} :: {s.regExSubType,-30} :: {s.regExString,-100} ";

                    GlobalVariables.WriteLineToDebugLog(myline);
                }
            }

            //for (int i=0; i< GlobalVariables.SBSetupHelper.regExUse.Count; i++)
            //{
            //    GlobalVariables.WriteLineToDebugLog("RegEx Use :: "+ GlobalVariables.SBSetupHelper.regExUse[i] + " :: " + GlobalVariables.SBSetupHelper.regExDesc[i]);
            //}
            
            ActGlobals.oFormActMain.BeforeLogLineRead -= ParseLine;
        }

        public void InitPlugin(System.Windows.Forms.TabPage pluginScreenSpace, System.Windows.Forms.Label pluginStatusText)
        {
            DateTime currentDateTime = DateTime.Now;

            if (!Directory.Exists(GlobalVariables.SBLogDir))
            {
                try
                {
                    // Attempt to create the directory
                    Directory.CreateDirectory(GlobalVariables.SBLogDir);
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur
                    GlobalVariables.WriteLineToDebugLog($"Error: {ex.Message}");
                }
            }

            GlobalVariables.WriteLineToDebugLog("InitPlugin !!!!");
            GlobalVariables.WriteLineToDebugLog("Version  :: " + GlobalVariables.version);
            GlobalVariables.WriteLineToDebugLog("DateTime :: " + currentDateTime);

            GlobalVariables.WriteLineToMissedParse("Version  :: " + GlobalVariables.version);
            GlobalVariables.WriteLineToMissedParse("DateTime :: " + currentDateTime);

            GlobalVariables.WriteLineToUnknownParse("Version  :: " + GlobalVariables.version);
            GlobalVariables.WriteLineToUnknownParse("DateTime :: " + currentDateTime);

            GlobalVariables.WriteLineToUnknownPBLog("Version  :: " + GlobalVariables.version);
            GlobalVariables.WriteLineToUnknownPBLog("DateTime :: " + currentDateTime);

            //Initialize the reg ex debug logs
            foreach (List<SBregExUsage> lSBreu in GlobalVariables.SBSetupHelper.allRegEx)
            {
                foreach (SBregExUsage s in lSBreu)
                {
                    s.InitLog();
                }
            }

            // Add Global Pre Parsing Helpers Here
            SBregExHelperFuncs.GlobalPreParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PreParse_CaseReplace);
            SBregExHelperFuncs.GlobalPreParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PreParse_AreIs);

            // Add Global Post Parsing Helpers Here
            SBregExHelperFuncs.GlobalPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
            SBregExHelperFuncs.GlobalPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);

            this.SetupSBEnvironment();
            ActGlobals.oFormActMain.LogPathHasCharName = false;
            ActGlobals.oFormActMain.LogFileFilter = "*.txt";
            ActGlobals.oFormActMain.ResetCheckLogs();

            ActGlobals.oFormActMain.BeforeLogLineRead += new LogLineEventDelegate(ParseLine);
            ActGlobals.oFormActMain.GetDateTimeFromLog = new FormActMain.DateTimeLogParser(ParseDateTime);
            ActGlobals.oFormActMain.LogFileChanged += new LogFileChangedDelegate(oFormActMain_LogFileChanged);

            
        }

        private string GetIntCommas()
        {
            return ActGlobals.mainTableShowCommas ? "#,0" : "0";
        }
        private string GetFloatCommas()
        {
            return ActGlobals.mainTableShowCommas ? "#,0.00" : "0.00";
        }

        private string EncounterFormatSwitch(EncounterData Data, List<CombatantData> SelectiveAllies, string VarName, string Extra)
        {
            long damage = 0;
            long healed = 0;
            int swings = 0;
            int hits = 0;
            int crits = 0;
            int heals = 0;
            int critheals = 0;
            //int cures = 0;
            int misses = 0;
            int hitfail = 0;
            float tohit = 0;
            float dps = 0;
            float hps = 0;
            long healstaken = 0;
            long damagetaken = 0;
            //int powerdrain = 0;
            long powerheal = 0;
            int kills = 0;
            int deaths = 0;

            switch (VarName)
            {
                case "maxheal":
                    return Data.GetMaxHeal(true, false);
                case "MAXHEAL":
                    return Data.GetMaxHeal(false, false);
                case "maxhealward":
                    return Data.GetMaxHeal(true, true);
                case "MAXHEALWARD":
                    return Data.GetMaxHeal(false, true);
                case "maxhit":
                    return Data.GetMaxHit(true);
                case "MAXHIT":
                    return Data.GetMaxHit(false);
                case "duration":
                    return Data.DurationS;
                case "DURATION":
                    return Data.Duration.TotalSeconds.ToString("0");
                case "damage":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    return damage.ToString();
                case "healed":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    return healed.ToString();
                case "swings":
                    foreach (CombatantData cd in SelectiveAllies)
                        swings += cd.Swings;
                    return swings.ToString();
                case "hits":
                    foreach (CombatantData cd in SelectiveAllies)
                        hits += cd.Hits;
                    return hits.ToString();
                case "crithits":
                    foreach (CombatantData cd in SelectiveAllies)
                        crits += cd.CritHits;
                    return crits.ToString();
                case "crithit%":
                    foreach (CombatantData cd in SelectiveAllies)
                        crits += cd.CritHits;
                    foreach (CombatantData cd in SelectiveAllies)
                        hits += cd.Hits;
                    float critdamperc = (float)crits / (float)hits;
                    return critdamperc.ToString("0'%");
                case "heals":
                    foreach (CombatantData cd in SelectiveAllies)
                        heals += cd.Heals;
                    return heals.ToString();
                case "critheals":
                    foreach (CombatantData cd in SelectiveAllies)
                        critheals += cd.CritHits;
                    return critheals.ToString();
                case "critheal%":
                    foreach (CombatantData cd in SelectiveAllies)
                        critheals += cd.CritHeals;
                    foreach (CombatantData cd in SelectiveAllies)
                        heals += cd.Heals;
                    float crithealperc = (float)critheals / (float)heals;
                    return crithealperc.ToString("0'%");
                //                case "cures":
                //                    foreach (CombatantData cd in SelectiveAllies)
                //                        cures += cd.CureDispels;
                //                    return cures.ToString();
                case "misses":
                    foreach (CombatantData cd in SelectiveAllies)
                        misses += cd.Misses;
                    return misses.ToString();
                case "hitfailed":
                    foreach (CombatantData cd in SelectiveAllies)
                        hitfail += cd.Blocked;
                    return hitfail.ToString();
                case "TOHIT":
                    foreach (CombatantData cd in SelectiveAllies)
                        tohit += cd.ToHit;
                    tohit /= SelectiveAllies.Count;
                    return tohit.ToString("0");
                case "DPS":
                case "ENCDPS":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / (float)Data.Duration.TotalSeconds;
                    return dps.ToString("0");
                case "ENCHPS":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / (float)Data.Duration.TotalSeconds;
                    return hps.ToString("0");
                case "tohit":
                    foreach (CombatantData cd in SelectiveAllies)
                        tohit += cd.ToHit;
                    tohit /= SelectiveAllies.Count;
                    return tohit.ToString("F");
                case "dps":
                case "encdps":
                    foreach (CombatantData cd in SelectiveAllies)
                        damage += cd.Damage;
                    dps = damage / (float)Data.Duration.TotalSeconds;
                    return dps.ToString("F");
                case "enchps":
                    foreach (CombatantData cd in SelectiveAllies)
                        healed += cd.Healed;
                    hps = healed / (float)Data.Duration.TotalSeconds;
                    return hps.ToString("F");
                case "healstaken":
                    foreach (CombatantData cd in SelectiveAllies)
                        healstaken += cd.HealsTaken;
                    return healstaken.ToString();
                case "damagetaken":
                    foreach (CombatantData cd in SelectiveAllies)
                        damagetaken += cd.DamageTaken;
                    return damagetaken.ToString();
                //                case "powerdrain":
                //                    foreach (CombatantData cd in SelectiveAllies)
                //                        powerdrain += cd.PowerDamage;
                //                    return powerdrain.ToString();
                case "powerheal":
                    foreach (CombatantData cd in SelectiveAllies)
                        powerheal += cd.PowerReplenish;
                    return powerheal.ToString();
                case "kills":
                    foreach (CombatantData cd in SelectiveAllies)
                        kills += cd.Kills;
                    return kills.ToString();
                case "deaths":
                    foreach (CombatantData cd in SelectiveAllies)
                        deaths += cd.Deaths;
                    return deaths.ToString();
                case "title":
                    return Data.Title;

                default:
                    return VarName;
            }
        }

        private string CombatantFormatSwitch(CombatantData Data, string VarName, string Extra)
        {
            int len = 0;
            switch (VarName)
            {
                case "name":
                    return Data.Name;
                case "NAME":
                    len = Int32.Parse(Extra);
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME3":
                    len = 3;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME4":
                    len = 4;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME5":
                    len = 5;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME6":
                    len = 6;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME7":
                    len = 7;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME8":
                    len = 8;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME9":
                    len = 9;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME10":
                    len = 10;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME11":
                    len = 11;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME12":
                    len = 12;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME13":
                    len = 13;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME14":
                    len = 14;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "NAME15":
                    len = 15;
                    return Data.Name.Length - len > 0 ? Data.Name.Remove(len, Data.Name.Length - len).Trim() : Data.Name;
                case "DURATION":
                    return Data.Duration.TotalSeconds.ToString("0");
                case "duration":
                    return Data.DurationS;
                case "maxhit":
                    return Data.GetMaxHit(true);
                case "MAXHIT":
                    return Data.GetMaxHit(false);
                case "maxheal":
                    return Data.GetMaxHeal(true, false);
                case "MAXHEAL":
                    return Data.GetMaxHeal(false, false);
                case "maxhealward":
                    return Data.GetMaxHeal(true, true);
                case "MAXHEALWARD":
                    return Data.GetMaxHeal(false, true);
                case "damage":
                    return Data.Damage.ToString();
                case "healed":
                    return Data.Healed.ToString();
                case "swings":
                    return Data.Swings.ToString();
                case "hits":
                    return Data.Hits.ToString();
                case "crithits":
                    return Data.CritHits.ToString();
                case "critheals":
                    return Data.CritHeals.ToString();
                case "crithit%":
                    return Data.CritDamPerc.ToString("0'%");
                case "critheal%":
                    return Data.CritHealPerc.ToString("0'%");
                case "heals":
                    return Data.Heals.ToString();
                //                case "cures":
                //                    return Data.CureDispels.ToString();
                case "misses":
                    return Data.Misses.ToString();
                case "hitfailed":
                    return Data.Blocked.ToString();
                case "TOHIT":
                    return Data.ToHit.ToString("0");
                case "DPS":
                    return Data.DPS.ToString("0");
                case "ENCDPS":
                    return Data.EncDPS.ToString("0");
                case "ENCHPS":
                    return Data.EncHPS.ToString("0");
                case "tohit":
                    return Data.ToHit.ToString("F");
                case "dps":
                    return Data.DPS.ToString("F");
                case "encdps":
                    return Data.EncDPS.ToString("F");
                case "enchps":
                    return Data.EncHPS.ToString("F");
                case "healstaken":
                    return Data.HealsTaken.ToString();
                case "damagetaken":
                    return Data.DamageTaken.ToString();
                //                case "powerdrain":
                //                    return Data.PowerDamage.ToString();
                case "powerheal":
                    return Data.PowerReplenish.ToString();
                case "kills":
                    return Data.Kills.ToString();
                case "deaths":
                    return Data.Deaths.ToString();
                case "damage%":
                    return Data.DamagePercent;
                case "healed%":
                    return Data.HealedPercent;
                //                case "threatstr":
                //                    return Data.GetThreatStr("Threat (Out)");
                //                case "threatdelta":
                //                    return Data.GetThreatDelta("Threat (Out)").ToString();
                case "n":
                    return "\n";
                case "t":
                    return "\t";

                default:
                    return VarName;
            }
        }

        private string GetDamageTypeGrouping(DamageTypeData Data)
        {
            string grouping = string.Empty;

            int swingTypeIndex = 0;
            if (Data.Outgoing)
            {
                grouping += "attacker=" + Data.Parent.Name;
                foreach (KeyValuePair<int, List<string>> links in CombatantData.SwingTypeToDamageTypeDataLinksOutgoing)
                {
                    foreach (string damageTypeLabel in links.Value)
                    {
                        if (Data.Type == damageTypeLabel)
                        {
                            grouping += String.Format("&swingtype{0}={1}", swingTypeIndex++ == 0 ? string.Empty : swingTypeIndex.ToString(), links.Key);
                        }
                    }
                }
            }
            else
            {
                grouping += "victim=" + Data.Parent.Name;
                foreach (KeyValuePair<int, List<string>> links in CombatantData.SwingTypeToDamageTypeDataLinksIncoming)
                {
                    foreach (string damageTypeLabel in links.Value)
                    {
                        if (Data.Type == damageTypeLabel)
                        {
                            grouping += String.Format("&swingtype{0}={1}", swingTypeIndex++ == 0 ? string.Empty : swingTypeIndex.ToString(), links.Key);
                        }
                    }
                }
            }

            return grouping;
        }
        private string GetAttackTypeSwingType(AttackType Data)
        {
            int swingType = 100;
            List<int> swingTypes = new List<int>();
            List<MasterSwing> cachedItems = new List<MasterSwing>(Data.Items);
            for (int i = 0; i < cachedItems.Count; i++)
            {
                MasterSwing s = cachedItems[i];
                if (swingTypes.Contains(s.SwingType) == false)
                    swingTypes.Add(s.SwingType);
            }
            if (swingTypes.Count == 1)
                swingType = swingTypes[0];

            return swingType.ToString();
        }
        private string GetCellDataSpecial(MasterSwing Data)
        {
            return Data.Special;
        }
        private string GetSqlDataSpecial(MasterSwing Data)
        {
            return Data.Special;
        }
        private int MasterSwingCompareSpecial(MasterSwing Left, MasterSwing Right)
        {
            return Left.Special.CompareTo(Right.Special);
        }

        private void SetupSBEnvironment()
        {


            CultureInfo usCulture = new CultureInfo("en-US");	// This is for SQL syntax; do not change

            EncounterData.ColumnDefs.Clear();
            // Do not change the SqlDataName for localization
            EncounterData.ColumnDefs.Add("EncId", new EncounterData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.EncId; }));
            EncounterData.ColumnDefs.Add("Title", new EncounterData.ColumnDef("Title", true, "VARCHAR(64)", "Title", (Data) => { return Data.Title; }, (Data) => { return Data.Title; }));
            EncounterData.ColumnDefs.Add("StartTime", new EncounterData.ColumnDef("StartTime", true, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : String.Format("{0} {1}", Data.StartTime.ToShortDateString(), Data.StartTime.ToLongTimeString()); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            EncounterData.ColumnDefs.Add("EndTime", new EncounterData.ColumnDef("EndTime", true, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            EncounterData.ColumnDefs.Add("Duration", new EncounterData.ColumnDef("Duration", true, "INT3", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }));
            EncounterData.ColumnDefs.Add("Damage", new EncounterData.ColumnDef("Damage", true, "INT4", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }));
            EncounterData.ColumnDefs.Add("EncDPS", new EncounterData.ColumnDef("EncDPS", true, "FLOAT4", "EncDPS", (Data) => { return Data.DPS.ToString(GetFloatCommas()); }, (Data) => { return Data.DPS.ToString(usCulture); }));
            //EncounterData.ColumnDefs.Add("Zone", new EncounterData.ColumnDef("Zone", false, "VARCHAR(64)", "Zone", (Data) => { return Data.ZoneName; }, (Data) => { return Data.ZoneName; }));
            EncounterData.ColumnDefs.Add("Kills", new EncounterData.ColumnDef("Kills", true, "INT3", "Kills", (Data) => { return Data.AlliedKills.ToString(GetIntCommas()); }, (Data) => { return Data.AlliedKills.ToString(); }));
            EncounterData.ColumnDefs.Add("Deaths", new EncounterData.ColumnDef("Deaths", true, "INT3", "Deaths", (Data) => { return Data.AlliedDeaths.ToString(); }, (Data) => { return Data.AlliedDeaths.ToString(); }));

            EncounterData.ExportVariables.Clear();
            EncounterData.ExportVariables.Add("n", new EncounterData.TextExportFormatter("n", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-newline"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-newline"].DisplayedText, (Data, SelectiveAllies, Extra) => { return "\n"; }));
            EncounterData.ExportVariables.Add("t", new EncounterData.TextExportFormatter("t", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-tab"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-tab"].DisplayedText, (Data, SelectiveAllies, Extra) => { return "\t"; }));
            EncounterData.ExportVariables.Add("title", new EncounterData.TextExportFormatter("title", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-title"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-title"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "title", Extra); }));
            EncounterData.ExportVariables.Add("duration", new EncounterData.TextExportFormatter("duration", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-duration"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-duration"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "duration", Extra); }));
            EncounterData.ExportVariables.Add("DURATION", new EncounterData.TextExportFormatter("DURATION", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-DURATION"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-DURATION"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DURATION", Extra); }));
            EncounterData.ExportVariables.Add("damage", new EncounterData.TextExportFormatter("damage", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damage"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damage"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damage", Extra); }));
            EncounterData.ExportVariables.Add("dps", new EncounterData.TextExportFormatter("dps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-dps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-dps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "dps", Extra); }));
            EncounterData.ExportVariables.Add("DPS", new EncounterData.TextExportFormatter("DPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-DPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-DPS"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "DPS", Extra); }));
            EncounterData.ExportVariables.Add("encdps", new EncounterData.TextExportFormatter("encdps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-extdps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-extdps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "encdps", Extra); }));
            EncounterData.ExportVariables.Add("ENCDPS", new EncounterData.TextExportFormatter("ENCDPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-EXTDPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-EXTDPS"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS", Extra); }));
            EncounterData.ExportVariables.Add("hits", new EncounterData.TextExportFormatter("hits", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-hits"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-hits"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "hits", Extra); }));
            //EncounterData.ExportVariables.Add("crithits", new EncounterData.TextExportFormatter("crithits", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-crithits"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-crithits"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "crithits", Extra); }));
            //EncounterData.ExportVariables.Add("crithit%", new EncounterData.TextExportFormatter("crithit%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-crithit%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-crithit%"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "crithit%", Extra); }));
            EncounterData.ExportVariables.Add("misses", new EncounterData.TextExportFormatter("misses", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-misses"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-misses"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "misses", Extra); }));
            //EncounterData.ExportVariables.Add("hitfailed", new EncounterData.TextExportFormatter("hitfailed", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-hitfailed"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-hitfailed"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "hitfailed", Extra); }));
            EncounterData.ExportVariables.Add("swings", new EncounterData.TextExportFormatter("swings", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-swings"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-swings"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "swings", Extra); }));
            //EncounterData.ExportVariables.Add("tohit", new EncounterData.TextExportFormatter("tohit", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-tohit"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-tohit"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "tohit", Extra); }));
            //EncounterData.ExportVariables.Add("TOHIT", new EncounterData.TextExportFormatter("TOHIT", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-TOHIT"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-TOHIT"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "TOHIT", Extra); }));
            EncounterData.ExportVariables.Add("maxhit", new EncounterData.TextExportFormatter("maxhit", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhit"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhit"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhit", Extra); }));
            EncounterData.ExportVariables.Add("MAXHIT", new EncounterData.TextExportFormatter("MAXHIT", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHIT"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHIT"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHIT", Extra); }));
            EncounterData.ExportVariables.Add("healed", new EncounterData.TextExportFormatter("healed", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healed"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healed"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "healed", Extra); }));
            EncounterData.ExportVariables.Add("enchps", new EncounterData.TextExportFormatter("enchps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-exthps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-exthps"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "enchps", Extra); }));
            EncounterData.ExportVariables.Add("ENCHPS", new EncounterData.TextExportFormatter("ENCHPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-EXTHPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-EXTHPS"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS", Extra); }));
            //EncounterData.ExportVariables.Add("critheals", new EncounterData.TextExportFormatter("critheals", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-critheals"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-critheals"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "critheals", Extra); }));
            //EncounterData.ExportVariables.Add("critheal%", new EncounterData.TextExportFormatter("critheal%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-critheal%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-critheal%"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "critheal%", Extra); }));
            EncounterData.ExportVariables.Add("heals", new EncounterData.TextExportFormatter("heals", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-heals"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-heals"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "heals", Extra); }));
            EncounterData.ExportVariables.Add("maxheal", new EncounterData.TextExportFormatter("maxheal", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxheal"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxheal"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxheal", Extra); }));
            EncounterData.ExportVariables.Add("MAXHEAL", new EncounterData.TextExportFormatter("MAXHEAL", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEAL"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEAL"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEAL", Extra); }));
            //EncounterData.ExportVariables.Add("maxhealward", new EncounterData.TextExportFormatter("maxhealward", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhealward"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhealward"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "maxhealward", Extra); }));
            //EncounterData.ExportVariables.Add("MAXHEALWARD", new EncounterData.TextExportFormatter("MAXHEALWARD", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEALWARD"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEALWARD"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEALWARD", Extra); }));
            EncounterData.ExportVariables.Add("damagetaken", new EncounterData.TextExportFormatter("damagetaken", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damagetaken"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damagetaken"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "damagetaken", Extra); }));
            EncounterData.ExportVariables.Add("healstaken", new EncounterData.TextExportFormatter("healstaken", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healstaken"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healstaken"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "healstaken", Extra); }));
            //EncounterData.ExportVariables.Add("powerheal", new EncounterData.TextExportFormatter("powerheal", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-powerheal"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerheal"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "powerheal", Extra); }));
            EncounterData.ExportVariables.Add("kills", new EncounterData.TextExportFormatter("kills", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-kills"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-kills"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "kills", Extra); }));
            EncounterData.ExportVariables.Add("deaths", new EncounterData.TextExportFormatter("deaths", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-deaths"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-deaths"].DisplayedText, (Data, SelectiveAllies, Extra) => { return EncounterFormatSwitch(Data, SelectiveAllies, "deaths", Extra); }));

            CombatantData.ColumnDefs.Clear();
            CombatantData.ColumnDefs.Add("EncId", new CombatantData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.EncId; }, (Left, Right) => { return 0; }));
            CombatantData.ColumnDefs.Add("Ally", new CombatantData.ColumnDef("Ally", false, "CHAR(1)", "Ally", (Data) => { return Data.Parent.GetAllies().Contains(Data).ToString(); }, (Data) => { return Data.Parent.GetAllies().Contains(Data) ? "T" : "F"; }, (Left, Right) => { return Left.Parent.GetAllies().Contains(Left).CompareTo(Right.Parent.GetAllies().Contains(Right)); }));
            CombatantData.ColumnDefs.Add("Name", new CombatantData.ColumnDef("Name", true, "VARCHAR(64)", "Name", (Data) => { return Data.Name; }, (Data) => { return Data.Name; }, (Left, Right) => { return Left.Name.CompareTo(Right.Name); }));
            CombatantData.ColumnDefs.Add("StartTime", new CombatantData.ColumnDef("StartTime", true, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.StartTime.CompareTo(Right.StartTime); }));
            CombatantData.ColumnDefs.Add("EndTime", new CombatantData.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.EndTime.CompareTo(Right.EndTime); }));
            CombatantData.ColumnDefs.Add("Duration", new CombatantData.ColumnDef("Duration", true, "INT3", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }, (Left, Right) => { return Left.Duration.CompareTo(Right.Duration); }));
            CombatantData.ColumnDefs.Add("Damage", new CombatantData.ColumnDef("Damage", true, "INT4", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            CombatantData.ColumnDefs.Add("Damage%", new CombatantData.ColumnDef("Damage%", true, "VARCHAR(4)", "DamagePerc", (Data) => { return Data.DamagePercent; }, (Data) => { return Data.DamagePercent; }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            CombatantData.ColumnDefs.Add("Kills", new CombatantData.ColumnDef("Kills", false, "INT3", "Kills", (Data) => { return Data.Kills.ToString(GetIntCommas()); }, (Data) => { return Data.Kills.ToString(); }, (Left, Right) => { return Left.Kills.CompareTo(Right.Kills); }));
            CombatantData.ColumnDefs.Add("Healed", new CombatantData.ColumnDef("Healed", false, "INT4", "Healed", (Data) => { return Data.Healed.ToString(GetIntCommas()); }, (Data) => { return Data.Healed.ToString(); }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
            CombatantData.ColumnDefs.Add("Healed%", new CombatantData.ColumnDef("Healed%", false, "VARCHAR(4)", "HealedPerc", (Data) => { return Data.HealedPercent; }, (Data) => { return Data.HealedPercent; }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
            //CombatantData.ColumnDefs.Add("CritHeals", new CombatantData.ColumnDef("CritHeals", false, "INT3", "CritHeals", (Data) => { return Data.CritHeals.ToString(GetIntCommas()); }, (Data) => { return Data.CritHeals.ToString(); }, (Left, Right) => { return Left.CritHeals.CompareTo(Right.CritHeals); }));
            CombatantData.ColumnDefs.Add("Heals", new CombatantData.ColumnDef("Heals", false, "INT3", "Heals", (Data) => { return Data.Heals.ToString(GetIntCommas()); }, (Data) => { return Data.Heals.ToString(); }, (Left, Right) => { return Left.Heals.CompareTo(Right.Heals); }));
            //CombatantData.ColumnDefs.Add("PowerReplenish", new CombatantData.ColumnDef("PowerReplenish", false, "INT4", "PowerReplenish", (Data) => { return Data.PowerReplenish.ToString(GetIntCommas()); }, (Data) => { return Data.PowerReplenish.ToString(); }, (Left, Right) => { return Left.PowerReplenish.CompareTo(Right.PowerReplenish); }));
            CombatantData.ColumnDefs.Add("DPS", new CombatantData.ColumnDef("DPS", false, "FLOAT4", "DPS", (Data) => { return Data.DPS.ToString(GetFloatCommas()); }, (Data) => { return Data.DPS.ToString(usCulture); }, (Left, Right) => { return Left.DPS.CompareTo(Right.DPS); }));
            CombatantData.ColumnDefs.Add("EncDPS", new CombatantData.ColumnDef("EncDPS", true, "FLOAT4", "EncDPS", (Data) => { return Data.EncDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.EncDPS.ToString(usCulture); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            CombatantData.ColumnDefs.Add("EncHPS", new CombatantData.ColumnDef("EncHPS", true, "FLOAT4", "EncHPS", (Data) => { return Data.EncHPS.ToString(GetFloatCommas()); }, (Data) => { return Data.EncHPS.ToString(usCulture); }, (Left, Right) => { return Left.Healed.CompareTo(Right.Healed); }));
            CombatantData.ColumnDefs.Add("Hits", new CombatantData.ColumnDef("Hits", false, "INT3", "Hits", (Data) => { return Data.Hits.ToString(GetIntCommas()); }, (Data) => { return Data.Hits.ToString(); }, (Left, Right) => { return Left.Hits.CompareTo(Right.Hits); }));
            //CombatantData.ColumnDefs.Add("CritHits", new CombatantData.ColumnDef("CritHits", false, "INT3", "CritHits", (Data) => { return Data.CritHits.ToString(GetIntCommas()); }, (Data) => { return Data.CritHits.ToString(); }, (Left, Right) => { return Left.CritHits.CompareTo(Right.CritHits); }));
            CombatantData.ColumnDefs.Add("Avoids", new CombatantData.ColumnDef("Avoids", false, "INT3", "Blocked", (Data) => { return Data.Blocked.ToString(GetIntCommas()); }, (Data) => { return Data.Blocked.ToString(); }, (Left, Right) => { return Left.Blocked.CompareTo(Right.Blocked); }));
            CombatantData.ColumnDefs.Add("Misses", new CombatantData.ColumnDef("Misses", false, "INT3", "Misses", (Data) => { return Data.Misses.ToString(GetIntCommas()); }, (Data) => { return Data.Misses.ToString(); }, (Left, Right) => { return Left.Misses.CompareTo(Right.Misses); }));
            CombatantData.ColumnDefs.Add("Swings", new CombatantData.ColumnDef("Swings", false, "INT3", "Swings", (Data) => { return Data.Swings.ToString(GetIntCommas()); }, (Data) => { return Data.Swings.ToString(); }, (Left, Right) => { return Left.Swings.CompareTo(Right.Swings); }));
            CombatantData.ColumnDefs.Add("HealingTaken", new CombatantData.ColumnDef("HealingTaken", false, "INT4", "HealsTaken", (Data) => { return Data.HealsTaken.ToString(GetIntCommas()); }, (Data) => { return Data.HealsTaken.ToString(); }, (Left, Right) => { return Left.HealsTaken.CompareTo(Right.HealsTaken); }));
            CombatantData.ColumnDefs.Add("DamageTaken", new CombatantData.ColumnDef("DamageTaken", true, "INT4", "DamageTaken", (Data) => { return Data.DamageTaken.ToString(GetIntCommas()); }, (Data) => { return Data.DamageTaken.ToString(); }, (Left, Right) => { return Left.DamageTaken.CompareTo(Right.DamageTaken); }));
            CombatantData.ColumnDefs.Add("Deaths", new CombatantData.ColumnDef("Deaths", true, "INT3", "Deaths", (Data) => { return Data.Deaths.ToString(GetIntCommas()); }, (Data) => { return Data.Deaths.ToString(); }, (Left, Right) => { return Left.Deaths.CompareTo(Right.Deaths); }));
            CombatantData.ColumnDefs.Add("ToHit%", new CombatantData.ColumnDef("ToHit%", false, "FLOAT4", "ToHit", (Data) => { return Data.ToHit.ToString(GetFloatCommas()); }, (Data) => { return Data.ToHit.ToString(usCulture); }, (Left, Right) => { return Left.ToHit.CompareTo(Right.ToHit); }));
            //CombatantData.ColumnDefs.Add("CritDam%", new CombatantData.ColumnDef("CritDam%", false, "VARCHAR(8)", "CritDamPerc", (Data) => { return Data.CritDamPerc.ToString("0'%"); }, (Data) => { return Data.CritDamPerc.ToString("0'%"); }, (Left, Right) => { return Left.CritDamPerc.CompareTo(Right.CritDamPerc); }));
            //CombatantData.ColumnDefs.Add("CritHeal%", new CombatantData.ColumnDef("CritHeal%", false, "VARCHAR(8)", "CritHealPerc", (Data) => { return Data.CritHealPerc.ToString("0'%"); }, (Data) => { return Data.CritHealPerc.ToString("0'%"); }, (Left, Right) => { return Left.CritHealPerc.CompareTo(Right.CritHealPerc); }));
            CombatantData.OutgoingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
        {
            {"Attack (Out)", new CombatantData.DamageTypeDef("Attack (Out)", -1, Color.DarkGoldenrod)},
            {"Cast (Out)", new CombatantData.DamageTypeDef("Cast (Out)", 0, Color.DarkOrange)},
            {"Outgoing Damage", new CombatantData.DamageTypeDef("Outgoing Damage", 0, Color.Orange)},
            {"Healed (Out)", new CombatantData.DamageTypeDef("Healed (Out)", 1, Color.Blue)},
            //{"Pet Healed (Out)", new CombatantData.DamageTypeDef("Pet Healed (Out)", 1, Color.Blue)},
            {"Outgoing Heal (Out)", new CombatantData.DamageTypeDef("Outgoing Heal (Out)", 1, Color.Blue)},
            {"Energy/Mana Replenish (Out)", new CombatantData.DamageTypeDef("Energy/Mana Replenish (Out)", 1, Color.Violet)},
            {"All Outgoing (Ref)", new CombatantData.DamageTypeDef("All Outgoing (Ref)", 0, Color.Black)}
        };
            CombatantData.IncomingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
        {
            {"Incoming Damage", new CombatantData.DamageTypeDef("Incoming Damage", -1, Color.Red)},
            {"Healed (Inc)",new CombatantData.DamageTypeDef("Healed (Inc)", 1, Color.LimeGreen)},
            {"Energy/Mana Replenish (Inc)",new CombatantData.DamageTypeDef("Energy/Mana Replenish (Inc)", 1, Color.MediumPurple)},
            {"All Incoming (Ref)",new CombatantData.DamageTypeDef("All Incoming (Ref)", 0, Color.Black)}
        };
            CombatantData.SwingTypeToDamageTypeDataLinksOutgoing = new SortedDictionary<int, List<string>>
        {
            {1, new List<string> { "Attack (Out)", "Outgoing Damage" } },
            {2, new List<string> { "Cast (Out)", "Outgoing Damage" } },
            {3, new List<string> { "Healed (Out)", "Outgoing Heal (Out)" } },
            //{4, new List<string> { "Pet Healed (Out)", "Outgoing Heal (Out)" } },
            {13, new List<string> { "Energy/Mana Replenish (Out)" } }
        };
            CombatantData.SwingTypeToDamageTypeDataLinksIncoming = new SortedDictionary<int, List<string>>
        {
            {1, new List<string> { "Incoming Damage" } },
            {2, new List<string> { "Incoming Damage" } },
            {3, new List<string> { "Healed (Inc)" } },
            {4, new List<string> { "Healed (Inc)" } },
            {13, new List<string> { "Energy/Mana Replenish (Inc)" } }
        };

            CombatantData.DamageSwingTypes = new List<int> { 1, 2 };
            CombatantData.HealingSwingTypes = new List<int> { 3, 4 };

            CombatantData.DamageTypeDataNonSkillDamage = "Attack (Out)";
            CombatantData.DamageTypeDataOutgoingDamage = "Outgoing Damage";
            CombatantData.DamageTypeDataOutgoingHealing = "Outgoing Heal (Out)";
            CombatantData.DamageTypeDataIncomingDamage = "Incoming Damage";
            CombatantData.DamageTypeDataIncomingHealing = "Healed (Inc)";

            CombatantData.ExportVariables.Clear();
            CombatantData.ExportVariables.Add("n", new CombatantData.TextExportFormatter("n", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-newline"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-newline"].DisplayedText, (Data, Extra) => { return "\n"; }));
            CombatantData.ExportVariables.Add("t", new CombatantData.TextExportFormatter("t", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-tab"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-tab"].DisplayedText, (Data, Extra) => { return "\t"; }));
            CombatantData.ExportVariables.Add("name", new CombatantData.TextExportFormatter("name", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-name"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-name"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "name", Extra); }));
            CombatantData.ExportVariables.Add("NAME", new CombatantData.TextExportFormatter("NAME", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-name"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-name"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME", Extra); }));
            CombatantData.ExportVariables.Add("duration", new CombatantData.TextExportFormatter("duration", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-duration"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-duration"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "duration", Extra); }));
            CombatantData.ExportVariables.Add("DURATION", new CombatantData.TextExportFormatter("DURATION", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-DURATION"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-DURATION"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "DURATION", Extra); }));
            CombatantData.ExportVariables.Add("damage", new CombatantData.TextExportFormatter("damage", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damage"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damage"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "damage", Extra); }));
            CombatantData.ExportVariables.Add("damage%", new CombatantData.TextExportFormatter("damage%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damage%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damage%"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "damage%", Extra); }));
            CombatantData.ExportVariables.Add("dps", new CombatantData.TextExportFormatter("dps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-dps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-dps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "dps", Extra); }));
            CombatantData.ExportVariables.Add("DPS", new CombatantData.TextExportFormatter("DPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-DPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-DPS"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "DPS", Extra); }));
            CombatantData.ExportVariables.Add("encdps", new CombatantData.TextExportFormatter("encdps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-extdps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-extdps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "encdps", Extra); }));
            CombatantData.ExportVariables.Add("ENCDPS", new CombatantData.TextExportFormatter("ENCDPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-EXTDPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-EXTDPS"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCDPS", Extra); }));
            CombatantData.ExportVariables.Add("hits", new CombatantData.TextExportFormatter("hits", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-hits"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-hits"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "hits", Extra); }));
            //CombatantData.ExportVariables.Add("crithits", new CombatantData.TextExportFormatter("crithits", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-crithits"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-crithits"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "crithits", Extra); }));
            //CombatantData.ExportVariables.Add("crithit%", new CombatantData.TextExportFormatter("crithit%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-crithit%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-crithit%"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "crithit%", Extra); }));
            CombatantData.ExportVariables.Add("misses", new CombatantData.TextExportFormatter("misses", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-misses"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-misses"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "misses", Extra); }));
            CombatantData.ExportVariables.Add("hitfailed", new CombatantData.TextExportFormatter("hitfailed", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-hitfailed"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-hitfailed"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "hitfailed", Extra); }));
            CombatantData.ExportVariables.Add("swings", new CombatantData.TextExportFormatter("swings", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-swings"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-swings"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "swings", Extra); }));
            CombatantData.ExportVariables.Add("tohit", new CombatantData.TextExportFormatter("tohit", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-tohit"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-tohit"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "tohit", Extra); }));
            CombatantData.ExportVariables.Add("TOHIT", new CombatantData.TextExportFormatter("TOHIT", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-TOHIT"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-TOHIT"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "TOHIT", Extra); }));
            CombatantData.ExportVariables.Add("maxhit", new CombatantData.TextExportFormatter("maxhit", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhit"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhit"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxhit", Extra); }));
            CombatantData.ExportVariables.Add("MAXHIT", new CombatantData.TextExportFormatter("MAXHIT", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHIT"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHIT"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHIT", Extra); }));
            CombatantData.ExportVariables.Add("healed", new CombatantData.TextExportFormatter("healed", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healed"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healed"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "healed", Extra); }));
            CombatantData.ExportVariables.Add("healed%", new CombatantData.TextExportFormatter("healed%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healed%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healed%"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "healed%", Extra); }));
            CombatantData.ExportVariables.Add("enchps", new CombatantData.TextExportFormatter("enchps", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-exthps"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-exthps"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "enchps", Extra); }));
            CombatantData.ExportVariables.Add("ENCHPS", new CombatantData.TextExportFormatter("ENCHPS", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-EXTHPS"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-EXTHPS"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "ENCHPS", Extra); }));
            //CombatantData.ExportVariables.Add("critheals", new CombatantData.TextExportFormatter("critheals", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-critheals"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-critheals"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "critheals", Extra); }));
            //CombatantData.ExportVariables.Add("critheal%", new CombatantData.TextExportFormatter("critheal%", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-critheal%"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-critheal%"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "critheal%", Extra); }));
            CombatantData.ExportVariables.Add("heals", new CombatantData.TextExportFormatter("heals", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-heals"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-heals"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "heals", Extra); }));
            CombatantData.ExportVariables.Add("maxheal", new CombatantData.TextExportFormatter("maxheal", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxheal"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxheal"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxheal", Extra); }));
            CombatantData.ExportVariables.Add("MAXHEAL", new CombatantData.TextExportFormatter("MAXHEAL", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEAL"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEAL"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHEAL", Extra); }));
            CombatantData.ExportVariables.Add("maxhealward", new CombatantData.TextExportFormatter("maxhealward", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-maxhealward"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-maxhealward"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "maxhealward", Extra); }));
            CombatantData.ExportVariables.Add("MAXHEALWARD", new CombatantData.TextExportFormatter("MAXHEALWARD", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-MAXHEALWARD"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-MAXHEALWARD"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "MAXHEALWARD", Extra); }));
            CombatantData.ExportVariables.Add("damagetaken", new CombatantData.TextExportFormatter("damagetaken", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-damagetaken"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-damagetaken"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "damagetaken", Extra); }));
            CombatantData.ExportVariables.Add("healstaken", new CombatantData.TextExportFormatter("healstaken", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-healstaken"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-healstaken"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "healstaken", Extra); }));
            CombatantData.ExportVariables.Add("powerheal", new CombatantData.TextExportFormatter("powerheal", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-powerheal"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-powerheal"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "powerheal", Extra); }));
            CombatantData.ExportVariables.Add("kills", new CombatantData.TextExportFormatter("kills", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-kills"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-kills"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "kills", Extra); }));
            CombatantData.ExportVariables.Add("deaths", new CombatantData.TextExportFormatter("deaths", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-deaths"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-deaths"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "deaths", Extra); }));
            CombatantData.ExportVariables.Add("NAME3", new CombatantData.TextExportFormatter("NAME3", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME3"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME3"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME3", Extra); }));
            CombatantData.ExportVariables.Add("NAME4", new CombatantData.TextExportFormatter("NAME4", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME4"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME4"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME4", Extra); }));
            CombatantData.ExportVariables.Add("NAME5", new CombatantData.TextExportFormatter("NAME5", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME5"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME5"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME5", Extra); }));
            CombatantData.ExportVariables.Add("NAME6", new CombatantData.TextExportFormatter("NAME6", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME6"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME6"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME6", Extra); }));
            CombatantData.ExportVariables.Add("NAME7", new CombatantData.TextExportFormatter("NAME7", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME7"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME7"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME7", Extra); }));
            CombatantData.ExportVariables.Add("NAME8", new CombatantData.TextExportFormatter("NAME8", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME8"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME8"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME8", Extra); }));
            CombatantData.ExportVariables.Add("NAME9", new CombatantData.TextExportFormatter("NAME9", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME9"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME9"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME9", Extra); }));
            CombatantData.ExportVariables.Add("NAME10", new CombatantData.TextExportFormatter("NAME10", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME10"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME10"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME10", Extra); }));
            CombatantData.ExportVariables.Add("NAME11", new CombatantData.TextExportFormatter("NAME11", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME11"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME11"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME11", Extra); }));
            CombatantData.ExportVariables.Add("NAME12", new CombatantData.TextExportFormatter("NAME12", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME12"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME12"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME12", Extra); }));
            CombatantData.ExportVariables.Add("NAME13", new CombatantData.TextExportFormatter("NAME13", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME13"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME13"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME13", Extra); }));
            CombatantData.ExportVariables.Add("NAME14", new CombatantData.TextExportFormatter("NAME14", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME14"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME14"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME14", Extra); }));
            CombatantData.ExportVariables.Add("NAME15", new CombatantData.TextExportFormatter("NAME15", ActGlobals.ActLocalization.LocalizationStrings["exportFormattingLabel-NAME15"].DisplayedText, ActGlobals.ActLocalization.LocalizationStrings["exportFormattingDesc-NAME15"].DisplayedText, (Data, Extra) => { return CombatantFormatSwitch(Data, "NAME15", Extra); }));

            DamageTypeData.ColumnDefs.Clear();
            DamageTypeData.ColumnDefs.Add("EncId", new DamageTypeData.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.Parent.EncId; }));
            DamageTypeData.ColumnDefs.Add("Combatant", new DamageTypeData.ColumnDef("Combatant", false, "VARCHAR(64)", "Combatant", (Data) => { return Data.Parent.Name; }, (Data) => { return Data.Parent.Name; }));
            DamageTypeData.ColumnDefs.Add("Grouping", new DamageTypeData.ColumnDef("Grouping", false, "VARCHAR(92)", "Grouping", (Data) => { return string.Empty; }, GetDamageTypeGrouping));
            DamageTypeData.ColumnDefs.Add("Type", new DamageTypeData.ColumnDef("Type", true, "VARCHAR(64)", "Type", (Data) => { return Data.Type; }, (Data) => { return Data.Type; }));
            DamageTypeData.ColumnDefs.Add("StartTime", new DamageTypeData.ColumnDef("StartTime", false, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            DamageTypeData.ColumnDefs.Add("EndTime", new DamageTypeData.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }));
            DamageTypeData.ColumnDefs.Add("Duration", new DamageTypeData.ColumnDef("Duration", false, "INT3", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }));
            DamageTypeData.ColumnDefs.Add("Damage", new DamageTypeData.ColumnDef("Damage", true, "INT4", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }));
            DamageTypeData.ColumnDefs.Add("EncDPS", new DamageTypeData.ColumnDef("EncDPS", true, "FLOAT4", "EncDPS", (Data) => { return Data.EncDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.EncDPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("CharDPS", new DamageTypeData.ColumnDef("CharDPS", false, "FLOAT4", "CharDPS", (Data) => { return Data.CharDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.CharDPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("DPS", new DamageTypeData.ColumnDef("DPS", false, "FLOAT4", "DPS", (Data) => { return Data.DPS.ToString(GetFloatCommas()); }, (Data) => { return Data.DPS.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("Average", new DamageTypeData.ColumnDef("Average", true, "FLOAT4", "Average", (Data) => { return Data.Average.ToString(GetFloatCommas()); }, (Data) => { return Data.Average.ToString(usCulture); }));
            DamageTypeData.ColumnDefs.Add("Median", new DamageTypeData.ColumnDef("Median", false, "INT3", "Median", (Data) => { return Data.Median.ToString(GetIntCommas()); }, (Data) => { return Data.Median.ToString(); }));
            DamageTypeData.ColumnDefs.Add("MinHit", new DamageTypeData.ColumnDef("MinHit", true, "INT3", "MinHit", (Data) => { return Data.MinHit.ToString(GetIntCommas()); }, (Data) => { return Data.MinHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("MaxHit", new DamageTypeData.ColumnDef("MaxHit", true, "INT3", "MaxHit", (Data) => { return Data.MaxHit.ToString(GetIntCommas()); }, (Data) => { return Data.MaxHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Hits", new DamageTypeData.ColumnDef("Hits", true, "INT3", "Hits", (Data) => { return Data.Hits.ToString(GetIntCommas()); }, (Data) => { return Data.Hits.ToString(); }));
            //DamageTypeData.ColumnDefs.Add("CritHits", new DamageTypeData.ColumnDef("CritHits", false, "INT3", "CritHits", (Data) => { return Data.CritHits.ToString(GetIntCommas()); }, (Data) => { return Data.CritHits.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Avoids", new DamageTypeData.ColumnDef("Avoids", false, "INT3", "Blocked", (Data) => { return Data.Blocked.ToString(GetIntCommas()); }, (Data) => { return Data.Blocked.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Misses", new DamageTypeData.ColumnDef("Misses", false, "INT3", "Misses", (Data) => { return Data.Misses.ToString(GetIntCommas()); }, (Data) => { return Data.Misses.ToString(); }));
            DamageTypeData.ColumnDefs.Add("Swings", new DamageTypeData.ColumnDef("Swings", true, "INT3", "Swings", (Data) => { return Data.Swings.ToString(GetIntCommas()); }, (Data) => { return Data.Swings.ToString(); }));
            DamageTypeData.ColumnDefs.Add("ToHit", new DamageTypeData.ColumnDef("ToHit", false, "FLOAT4", "ToHit", (Data) => { return Data.ToHit.ToString(GetFloatCommas()); }, (Data) => { return Data.ToHit.ToString(); }));
            DamageTypeData.ColumnDefs.Add("AvgDelay", new DamageTypeData.ColumnDef("AvgDelay", false, "FLOAT4", "AverageDelay", (Data) => { return Data.AverageDelay.ToString(GetFloatCommas()); }, (Data) => { return Data.AverageDelay.ToString(); }));
            //DamageTypeData.ColumnDefs.Add("Crit%", new DamageTypeData.ColumnDef("Crit%", true, "VARCHAR(8)", "CritPerc", (Data) => { return Data.CritPerc.ToString("0'%"); }, (Data) => { return Data.CritPerc.ToString("0'%"); }));

            AttackType.ColumnDefs.Clear();
            AttackType.ColumnDefs.Add("EncId", new AttackType.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.Parent.Parent.Parent.EncId; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("Attacker", new AttackType.ColumnDef("Attacker", false, "VARCHAR(64)", "Attacker", (Data) => { return Data.Parent.Outgoing ? Data.Parent.Parent.Name : string.Empty; }, (Data) => { return Data.Parent.Outgoing ? Data.Parent.Parent.Name : string.Empty; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("Victim", new AttackType.ColumnDef("Victim", false, "VARCHAR(64)", "Victim", (Data) => { return Data.Parent.Outgoing ? string.Empty : Data.Parent.Parent.Name; }, (Data) => { return Data.Parent.Outgoing ? string.Empty : Data.Parent.Parent.Name; }, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("SwingType", new AttackType.ColumnDef("SwingType", false, "INT1", "SwingType", GetAttackTypeSwingType, GetAttackTypeSwingType, (Left, Right) => { return 0; }));
            AttackType.ColumnDefs.Add("Type", new AttackType.ColumnDef("Type", true, "VARCHAR(64)", "Type", (Data) => { return Data.Type; }, (Data) => { return Data.Type; }, (Left, Right) => { return Left.Type.CompareTo(Right.Type); }));
            AttackType.ColumnDefs.Add("StartTime", new AttackType.ColumnDef("StartTime", false, "TIMESTAMP", "StartTime", (Data) => { return Data.StartTime == DateTime.MaxValue ? "--:--:--" : Data.StartTime.ToString("T"); }, (Data) => { return Data.StartTime == DateTime.MaxValue ? "0000-00-00 00:00:00" : Data.StartTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.StartTime.CompareTo(Right.StartTime); }));
            AttackType.ColumnDefs.Add("EndTime", new AttackType.ColumnDef("EndTime", false, "TIMESTAMP", "EndTime", (Data) => { return Data.EndTime == DateTime.MinValue ? "--:--:--" : Data.EndTime.ToString("T"); }, (Data) => { return Data.EndTime == DateTime.MinValue ? "0000-00-00 00:00:00" : Data.EndTime.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.EndTime.CompareTo(Right.EndTime); }));
            AttackType.ColumnDefs.Add("Duration", new AttackType.ColumnDef("Duration", false, "INT3", "Duration", (Data) => { return Data.DurationS; }, (Data) => { return Data.Duration.TotalSeconds.ToString("0"); }, (Left, Right) => { return Left.Duration.CompareTo(Right.Duration); }));
            AttackType.ColumnDefs.Add("Damage", new AttackType.ColumnDef("Damage", true, "INT4", "Damage", (Data) => { return Data.Damage.ToString(GetIntCommas()); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            AttackType.ColumnDefs.Add("EncDPS", new AttackType.ColumnDef("EncDPS", true, "FLOAT4", "EncDPS", (Data) => { return Data.EncDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.EncDPS.ToString(usCulture); }, (Left, Right) => { return Left.EncDPS.CompareTo(Right.EncDPS); }));
            AttackType.ColumnDefs.Add("CharDPS", new AttackType.ColumnDef("CharDPS", false, "FLOAT4", "CharDPS", (Data) => { return Data.CharDPS.ToString(GetFloatCommas()); }, (Data) => { return Data.CharDPS.ToString(usCulture); }, (Left, Right) => { return Left.CharDPS.CompareTo(Right.CharDPS); }));
            AttackType.ColumnDefs.Add("DPS", new AttackType.ColumnDef("DPS", false, "FLOAT4", "DPS", (Data) => { return Data.DPS.ToString(GetFloatCommas()); }, (Data) => { return Data.DPS.ToString(usCulture); }, (Left, Right) => { return Left.DPS.CompareTo(Right.DPS); }));
            AttackType.ColumnDefs.Add("Average", new AttackType.ColumnDef("Average", true, "FLOAT4", "Average", (Data) => { return Data.Average.ToString(GetFloatCommas()); }, (Data) => { return Data.Average.ToString(usCulture); }, (Left, Right) => { return Left.Average.CompareTo(Right.Average); }));
            AttackType.ColumnDefs.Add("Median", new AttackType.ColumnDef("Median", true, "INT3", "Median", (Data) => { return Data.Median.ToString(GetIntCommas()); }, (Data) => { return Data.Median.ToString(); }, (Left, Right) => { return Left.Median.CompareTo(Right.Median); }));
            AttackType.ColumnDefs.Add("MinHit", new AttackType.ColumnDef("MinHit", true, "INT3", "MinHit", (Data) => { return Data.MinHit.ToString(GetIntCommas()); }, (Data) => { return Data.MinHit.ToString(); }, (Left, Right) => { return Left.MinHit.CompareTo(Right.MinHit); }));
            AttackType.ColumnDefs.Add("MaxHit", new AttackType.ColumnDef("MaxHit", true, "INT3", "MaxHit", (Data) => { return Data.MaxHit.ToString(GetIntCommas()); }, (Data) => { return Data.MaxHit.ToString(); }, (Left, Right) => { return Left.MaxHit.CompareTo(Right.MaxHit); }));
            AttackType.ColumnDefs.Add("Resist", new AttackType.ColumnDef("Resist", true, "VARCHAR(64)", "Resist", (Data) => { return Data.Resist; }, (Data) => { return Data.Resist; }, (Left, Right) => { return Left.Resist.CompareTo(Right.Resist); }));
            AttackType.ColumnDefs.Add("Hits", new AttackType.ColumnDef("Hits", true, "INT3", "Hits", (Data) => { return Data.Hits.ToString(GetIntCommas()); }, (Data) => { return Data.Hits.ToString(); }, (Left, Right) => { return Left.Hits.CompareTo(Right.Hits); }));
            //AttackType.ColumnDefs.Add("CritHits", new AttackType.ColumnDef("CritHits", false, "INT3", "CritHits", (Data) => { return Data.CritHits.ToString(GetIntCommas()); }, (Data) => { return Data.CritHits.ToString(); }, (Left, Right) => { return Left.CritHits.CompareTo(Right.CritHits); }));
            AttackType.ColumnDefs.Add("Avoids", new AttackType.ColumnDef("Avoids", false, "INT3", "Blocked", (Data) => { return Data.Blocked.ToString(GetIntCommas()); }, (Data) => { return Data.Blocked.ToString(); }, (Left, Right) => { return Left.Blocked.CompareTo(Right.Blocked); }));
            AttackType.ColumnDefs.Add("Misses", new AttackType.ColumnDef("Misses", false, "INT3", "Misses", (Data) => { return Data.Misses.ToString(GetIntCommas()); }, (Data) => { return Data.Misses.ToString(); }, (Left, Right) => { return Left.Misses.CompareTo(Right.Misses); }));
            AttackType.ColumnDefs.Add("Swings", new AttackType.ColumnDef("Swings", true, "INT3", "Swings", (Data) => { return Data.Swings.ToString(GetIntCommas()); }, (Data) => { return Data.Swings.ToString(); }, (Left, Right) => { return Left.Swings.CompareTo(Right.Swings); }));
            AttackType.ColumnDefs.Add("ToHit", new AttackType.ColumnDef("ToHit", true, "FLOAT4", "ToHit", (Data) => { return Data.ToHit.ToString(GetFloatCommas()); }, (Data) => { return Data.ToHit.ToString(usCulture); }, (Left, Right) => { return Left.ToHit.CompareTo(Right.ToHit); }));
            AttackType.ColumnDefs.Add("AvgDelay", new AttackType.ColumnDef("AvgDelay", false, "FLOAT4", "AverageDelay", (Data) => { return Data.AverageDelay.ToString(GetFloatCommas()); }, (Data) => { return Data.AverageDelay.ToString(usCulture); }, (Left, Right) => { return Left.AverageDelay.CompareTo(Right.AverageDelay); }));
            //AttackType.ColumnDefs.Add("Crit%", new AttackType.ColumnDef("Crit%", true, "VARCHAR(8)", "CritPerc", (Data) => { return Data.CritPerc.ToString("0'%"); }, (Data) => { return Data.CritPerc.ToString("0'%"); }, (Left, Right) => { return Left.CritPerc.CompareTo(Right.CritPerc); }));

            MasterSwing.ColumnDefs.Clear();
            MasterSwing.ColumnDefs.Add("EncId", new MasterSwing.ColumnDef("EncId", false, "CHAR(8)", "EncId", (Data) => { return string.Empty; }, (Data) => { return Data.ParentEncounter.EncId; }, (Left, Right) => { return 0; }));
            MasterSwing.ColumnDefs.Add("Time", new MasterSwing.ColumnDef("Time", true, "TIMESTAMP", "STime", (Data) => { return Data.Time.ToString("T"); }, (Data) => { return Data.Time.ToString("u").TrimEnd(new char[] { 'Z' }); }, (Left, Right) => { return Left.Time.CompareTo(Right.Time); }));
            MasterSwing.ColumnDefs.Add("Attacker", new MasterSwing.ColumnDef("Attacker", true, "VARCHAR(64)", "Attacker", (Data) => { return Data.Attacker; }, (Data) => { return Data.Attacker; }, (Left, Right) => { return Left.Attacker.CompareTo(Right.Attacker); }));
            MasterSwing.ColumnDefs.Add("SwingType", new MasterSwing.ColumnDef("SwingType", false, "INT1", "SwingType", (Data) => { return Data.SwingType.ToString(); }, (Data) => { return Data.SwingType.ToString(); }, (Left, Right) => { return Left.SwingType.CompareTo(Right.SwingType); }));
            MasterSwing.ColumnDefs.Add("AttackType", new MasterSwing.ColumnDef("AttackType", true, "VARCHAR(64)", "AttackType", (Data) => { return Data.AttackType; }, (Data) => { return Data.AttackType; }, (Left, Right) => { return Left.AttackType.CompareTo(Right.AttackType); }));
            MasterSwing.ColumnDefs.Add("DamageType", new MasterSwing.ColumnDef("DamageType", true, "VARCHAR(64)", "DamageType", (Data) => { return Data.DamageType; }, (Data) => { return Data.DamageType; }, (Left, Right) => { return Left.DamageType.CompareTo(Right.DamageType); }));
            MasterSwing.ColumnDefs.Add("Victim", new MasterSwing.ColumnDef("Victim", true, "VARCHAR(64)", "Victim", (Data) => { return Data.Victim; }, (Data) => { return Data.Victim; }, (Left, Right) => { return Left.Victim.CompareTo(Right.Victim); }));
            MasterSwing.ColumnDefs.Add("DamageNum", new MasterSwing.ColumnDef("DamageNum", false, "INT3", "Damage", (Data) => { return ((int)Data.Damage).ToString(); }, (Data) => { return ((int)Data.Damage).ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            MasterSwing.ColumnDefs.Add("Damage", new MasterSwing.ColumnDef("Damage", true, "VARCHAR(128)", "DamageString", /* lambda */ (Data) => { return Data.Damage.ToString(); }, (Data) => { return Data.Damage.ToString(); }, (Left, Right) => { return Left.Damage.CompareTo(Right.Damage); }));
            // As a C# lesson, the above lines(lambda expressions) can also be written as(anonymous methods):
            //MasterSwing.ColumnDefs.Add("Critical", new MasterSwing.ColumnDef("Critical", true, "CHAR(1)", "Critical", /* anonymous */ delegate (MasterSwing Data) { return Data.Critical.ToString(); }, delegate (MasterSwing Data) { return Data.Critical.ToString(usCulture)[0].ToString(); }, delegate (MasterSwing Left, MasterSwing Right) { return Left.Critical.CompareTo(Right.Critical); }));
            // Or also written as(delegated methods):
            MasterSwing.ColumnDefs.Add("Special", new MasterSwing.ColumnDef("Special", true, "VARCHAR(64)", "Special", /* delegate */ GetCellDataSpecial, GetSqlDataSpecial, MasterSwingCompareSpecial));


            ActGlobals.oFormActMain.ValidateLists();
            ActGlobals.oFormActMain.ValidateTableSetup();
            ActGlobals.oFormActMain.TimeStampLen = 14;

            // All encounters are set by Enter/ExitCombat.
            UserControl opMainTableGen = (UserControl)ActGlobals.oFormActMain.OptionsControlSets[@"Main Table/Encounters\General"][0];
            CheckBox cbIdleEnd = (CheckBox)opMainTableGen.Controls["cbIdleEnd"];
            cbIdleEnd.Checked = false;
        }

        

        private void ParseLine(bool isImport, LogLineEventArgs log)
        {
            int actGTS = ActGlobals.oFormActMain.GlobalTimeSorter++;
            log.detectedType = Color.Black.ToArgb();
            DateTime time = ActGlobals.oFormActMain.LastKnownTime;
            
            GlobalVariables.WriteLineToDebugLog(log.logLine);

            LogLine line = new LogLine(log.logLine, actGTS);

            if (line.valid)
            {
                /*if (log.logLine.Contains("{836045448945490}")) // Exit Combat
                {
                    ActGlobals.oFormActMain.EndCombat(!isImport);
                    log.detectedType = Color.Purple.ToArgb();
                    return;
                }*/

                /*if (log.logLine.Contains("{836045448945489}")) // Enter Combat
                {
                    ActGlobals.oFormActMain.EndCombat(!isImport);
                    ActGlobals.charName = line.source;
                    ActGlobals.oFormActMain.SetEncounter(time, line.source, line.target);
                    log.detectedType = Color.Purple.ToArgb();
                    return;
                } */

                int type = 0;
                /*if (log.logLine.Contains("{836045448945501}")) // Damage
                {
                    log.detectedType = Color.Red.ToArgb();
                    type = DMG;
                }
                else if (log.logLine.Contains("{836045448945488}") || // Taunt
                    log.logLine.Contains("{836045448945483}")) // Threat
                {
                    log.detectedType = Color.Blue.ToArgb();
                    type = THREAT;
                }*/

                //if (line.event_type.Equals("heal")) // Heals
                switch (line.value_type)
                {
                    case "suffer":
                    case "parry":
                    case "dodge":
                    case "block":
                    case "miss":
                    case "hit":
                        if (line.event_type.Equals("Stamina") || line.event_type.Equals("stamina") || line.event_type.Equals("mana") || line.event_type.Equals("Mana"))
                        {
                            log.detectedType = Color.Yellow.ToArgb();
                            type = 13;
                        }
                        else 
                        { 
                            log.detectedType = Color.Blue.ToArgb();
                            //type = DMG;
                            type = (int)SwingTypeEnum.Melee;
                        }
                        break;

                    case "cry":
                        log.detectedType = Color.Cyan.ToArgb();
                        type = (int)SwingTypeEnum.NonMelee;
                        break;

                    case "active":
                        log.detectedType = Color.Cyan.ToArgb();
                        line.ability = line.event_type + " " + line.ability;
                        type = (int)SwingTypeEnum.Melee;
                        break;

                    case "slash":
                    case "buffet":
                    case "impale":
                    case "damage":
                    case "blast":
                    case "engulf":
                    case "burn":
                    case "bleed":
                    case "poison":
                    case "freeze":
                    case "strike":
                    case "fire":
                    case "lightning":
                    case "electrocute":
                    case "take":
                    case "smite":
                    case "shock":
                    case "hurt":
                        if (line.ability.Equals("resist"))
                        {
                            log.detectedType = Color.Yellow.ToArgb();
                            type = (int)SwingTypeEnum.NonMelee;
                        } 
                        else 
                        {
                            log.detectedType = Color.Orange.ToArgb();
                            //type = DMG;
                            //type = (int)SwingTypeEnum.NonMelee;
                            type = (int)SwingTypeEnum.Melee;
                        }
                        break;

                    case "drain":
                        if (line.event_type.Equals("mana"))
                        {
                            log.detectedType = Color.Green.ToArgb();
                            type = 13;
                        }
                        else if (line.event_type.Equals("health"))
                        {
                            log.detectedType = Color.LightCoral.ToArgb();
                            if (line.source.Equals(line.target))
                            {
                                type = (int)SwingTypeEnum.Healing;
                            }
                            else
                            {
                                type = (int)SwingTypeEnum.Melee;
                            }
                        }
                        else
                        {
                            log.detectedType = Color.Orange.ToArgb();
                            type = (int)SwingTypeEnum.Melee;
                        }
                        break;

                    case "surround":
                        log.detectedType = Color.Red.ToArgb();
                        type = (int)SwingTypeEnum.Healing;
                        break;

                    case "heal":
                        if (line.ability.Equals("Relgor's Restorative Elixir") | line.ability.Equals("Caster Oil") | line.event_type.Equals("stamina"))
                        {
                            log.detectedType = Color.Green.ToArgb();
                            type = 13;
                        }
                        else
                        {
                            log.detectedType = Color.Green.ToArgb();
                            //type = HEALS;
                            type = (int)SwingTypeEnum.Healing;
                        }
                        break;

                    case "enter":
                    case "execute":
                    case "assume":
                    case "use":
                    case "cast":
                        log.detectedType = Color.Green.ToArgb();
                        //type = HEALS;
                        type = (int)SwingTypeEnum.NonMelee;
                        break;

                    case "kicks dirt":
                    case "attack":
                        log.detectedType = Color.Blue.ToArgb();
                        //type = THREAT;
                        type = (int)SwingTypeEnum.NonMelee;
                        break;

                    case "fades":
                    case "returns":
                        log.detectedType |= Color.Purple.ToArgb();
                        type = (int)SwingTypeEnum.NonMelee;
                        break;

                    default:
                        type = 0; 
                        break;
                };
                

                /*else if (line.event_type.Contains("Restore"))
                {
                    log.detectedType = Color.OrangeRed.ToArgb();
                    type = 20;
                }
                else if (line.event_type.Contains("Spend"))
                {
                    log.detectedType = Color.Cyan.ToArgb();
                    type = 21;
                }
                if (line.ability != "")
                {
                    last_ability = line.ability;
                }
                if ((type == 20 || type == 21) && ActGlobals.oFormActMain.SetEncounter(time, line.source, line.target))
                {
                    ActGlobals.oFormActMain.AddCombatAction(type, line.crit_value, "None", line.source, last_ability, 
                        new Dnum(line.value), time, ActGlobals.oFormActMain.GlobalTimeSorter, line.target, "");
                }
                */

                /*
                if (!ActGlobals.oFormActMain.InCombat)
                {
                    return;
                }
                if (line.threat > 0 && ActGlobals.oFormActMain.SetEncounter(time, line.source, line.target))
                {
                    ActGlobals.oFormActMain.AddCombatAction(type, line.crit_value, "None", line.source, line.ability,
                        new Dnum(line.value), time, ActGlobals.oFormActMain.GlobalTimeSorter, line.target, line.value_type);
                    ActGlobals.oFormActMain.AddCombatAction(16, line.crit_value, "None", line.source, line.ability,
                        new Dnum(line.threat), time, ActGlobals.oFormActMain.GlobalTimeSorter, line.target, "Increase");
                }
                */

                if (ActGlobals.oFormActMain.InCombat == false)
                {
                    ActGlobals.oFormActMain.SetEncounter(time, line.source, line.target);
                    log.detectedType = Color.Purple.ToArgb();
                }



                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Type       = " + type.ToString());
                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Crit_value = " + line.crit_value.ToString());
                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Source     = " + line.source);
                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Ability    = " + line.ability);
                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Value      = " + line.value.ToString());
                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Time       = " + line.time);
                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Time_Sorter= " + ActGlobals.oFormActMain.GlobalTimeSorter);
                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Target     = " + line.target);
                GlobalVariables.WriteLineToDebugLog("AddCombatAction :: Value_Type = " + line.value_type);

                Dnum myDnum = null;

                switch (line.value_type)
                {
                    case "parry":
                        myDnum = new Dnum(Dnum.Miss, "Parry");
                        ActGlobals.oFormActMain.AddCombatAction(type, line.crit_value, "None", line.source, line.ability,
                                                                myDnum, time, ActGlobals.oFormActMain.GlobalTimeSorter,
                                                                line.target, line.value_type);
                        break;
                    case "dodge":
                        myDnum = new Dnum(Dnum.Miss, "Dodge");
                        ActGlobals.oFormActMain.AddCombatAction(type, line.crit_value, "None", line.source, line.ability,
                                                                myDnum, time, ActGlobals.oFormActMain.GlobalTimeSorter,
                                                                line.target, line.value_type);
                        break;
                    case "block":
                        myDnum = new Dnum(Dnum.Miss, "Block");
                        ActGlobals.oFormActMain.AddCombatAction(type, line.crit_value, "None", line.source, line.ability,
                                                                myDnum, time, ActGlobals.oFormActMain.GlobalTimeSorter,
                                                                line.target, line.value_type);
                        break;
                    case "miss":
                        ActGlobals.oFormActMain.AddCombatAction(type, line.crit_value, "None", line.source, line.ability,
                                                                Dnum.Miss, time, ActGlobals.oFormActMain.GlobalTimeSorter,
                                                                line.target, line.value_type);
                        break;


                    case "die":
                        type = (int)SwingTypeEnum.Melee;
                        ActGlobals.oFormActMain.AddCombatAction(type, line.crit_value, "None", line.source, "Killing Blow", 
                                                                Dnum.Death, time, ActGlobals.oFormActMain.GlobalTimeSorter, 
                                                                line.target, "Death");
                        break;

                    default :
                        ActGlobals.oFormActMain.AddCombatAction(type, line.crit_value, "None", line.source, line.ability,
                                                                new Dnum(line.value), time, ActGlobals.oFormActMain.GlobalTimeSorter, 
                                                                line.target, line.value_type);
                        break;    
                }
            
            }
            return;
        }
        DateTime logFileDate = DateTime.Now;
        Regex logfileDateTimeRegex = new Regex(@"combat_(?<Y>\d{4})-(?<M>\d\d)-(?<D>\d\d)_(?<h>\d\d)_(?<m>\d\d)_(?<s>\d\d)_\d+\.txt", RegexOptions.Compiled);
        void oFormActMain_LogFileChanged(bool IsImport, string NewLogFileName)
        {
            if (NewLogFileName == "")
            {
                return;
            }
            //combat_2012-04-02_09_20_30_162660.txt
            FileInfo newFile = new FileInfo(NewLogFileName);
            Match match = logfileDateTimeRegex.Match(newFile.Name);
            if (match.Success)	// If we can parse the creation date from the filename
            {
                try
                {
                    logFileDate = new DateTime(
                        Int32.Parse(match.Groups[1].Value),		// Y
                        Int32.Parse(match.Groups[2].Value),		// M
                        Int32.Parse(match.Groups[3].Value),		// D
                        Int32.Parse(match.Groups[4].Value),		// h
                        Int32.Parse(match.Groups[5].Value),		// m
                        Int32.Parse(match.Groups[6].Value));		// s
                }
                catch
                {
                    logFileDate = newFile.CreationTime;
                }
            }
            else
            {
                logFileDate = newFile.CreationTime;
            }
        }
        private DateTime ParseDateTime(string line)
        {
            try
            {
                //[22:55:28.335] 
                if (line.Length < ActGlobals.oFormActMain.TimeStampLen)
                    return ActGlobals.oFormActMain.LastEstimatedTime;

                int hour, min, sec, millis;

                hour = Convert.ToInt32(line.Substring(1, 2));
                min = Convert.ToInt32(line.Substring(4, 2));
                sec = Convert.ToInt32(line.Substring(7, 2));
                millis = Convert.ToInt32(line.Substring(10, 3));
                DateTime parsedTime = new DateTime(logFileDate.Year, logFileDate.Month, logFileDate.Day, hour, min, sec, millis);
                if (parsedTime < logFileDate)			// if time loops from 23h to 0h, the parsed time will be less than the log creation time, so add one day
                    parsedTime = parsedTime.AddDays(1);	// only works for log files that are less than 24h in duration

                return parsedTime;
            }
            catch
            {
                return ActGlobals.oFormActMain.LastEstimatedTime;
            }
        }
    }
    public class LogLine
    {
        public bool skipLog = false; 
        public bool valid = false;
        public string time = string.Empty;
        public string source = string.Empty;
        public string target = string.Empty;
        public string ability = string.Empty;
        public string event_type = string.Empty;
        public string event_detail = string.Empty;
        public bool crit_value = false;
        public int value = 0;
        public string value_type = string.Empty;
        public int threat = 0;
        public Point regExIndx = new Point(-1,-1);
        public int globalTimeSorter = 0;

        /*
        static Regex regex = 
            new Regex(@"\[(.*)\] \[(.*)\] \[(.*)\] \[(.*)\] \[(.*)\] \((.*)\)[\s<]*(\d*)?[>]*", 
                RegexOptions.Compiled);
        static Regex id_regex = new Regex(@"\s*\{\d*}\s*", RegexOptions.Compiled);
        */

        /*
        static Regex regexHeals =
            new Regex(@"\((\d*\:\d*\:\d*)\)\W*(.*)'s (.*) heals (.*) for (\d*) points.*",
                RegexOptions.Compiled);
        */
        //static Regex id_regex = new Regex(@"\s*\{\d*}\s*", RegexOptions.Compiled);

        public int GetConvertedValueFromString(string str)
        {
            int retVal = 0;

            try
            {
                retVal = Convert.ToInt32(str);
            }
            catch (FormatException ex)
            {
                GlobalVariables.WriteLineToDebugLog("FormatException :: Message    = " + ex.Message);
                GlobalVariables.WriteLineToDebugLog("FormatException :: dict[\"value\"] = " + str);
                GlobalVariables.WriteLineToDebugLog("FormatException :: Valid      = " + this.valid.ToString());
                GlobalVariables.WriteLineToDebugLog("FormatException :: Time       = " + this.time);
                GlobalVariables.WriteLineToDebugLog("FormatException :: Source     = " + this.source);
                GlobalVariables.WriteLineToDebugLog("FormatException :: Ability    = " + this.ability);
                GlobalVariables.WriteLineToDebugLog("FormatException :: Target     = " + this.target);
                GlobalVariables.WriteLineToDebugLog("FormatException :: Value      = " + retVal.ToString());
                GlobalVariables.WriteLineToDebugLog("FormatException :: Value_Type = " + this.value_type);
                GlobalVariables.WriteLineToDebugLog("FormatException :: Event_Type = " + this.event_type);
                GlobalVariables.WriteLineToDebugLog("FormatException :: Event_Deta = " + this.event_detail);
                GlobalVariables.WriteLineToDebugLog("FormatException :: RegExIndx  = " + this.regExIndx);
                GlobalVariables.WriteLineToDebugLog("FormatException :: RegExDesc  = " + GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExType + " :: " +
                                                                                         GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExSubType + " :: " +
                                                                                         GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExString);

                retVal = 0;
            }

            return retVal;
        }

        public LogLine(string line, int actGTS)
        {
            this.globalTimeSorter = actGTS;

            //Processing Global Pre Parse Functions (string Modifications)
            foreach (var GlobalPreParseFunc in SBregExHelperFuncs.GlobalPreParseFuncList)
            {
                line = GlobalPreParseFunc(line);
            }

            GlobalVariables.WriteLineToDebugLog(line);
            
            this.valid = false;
            
            //line = id_regex.Replace(line, "");
            //MatchCollection matchesHeal = regexHeals.Matches(line);
            Dictionary<string, string> dict = null;
            bool setFullBreak = false;
            //int whichRegex = -1;
            Point whichRegex = new Point(-1, -1);
            foreach (List<SBregExUsage> list in GlobalVariables.SBSetupHelper.allRegEx)
            {
                whichRegex.X++;
                whichRegex.Y = -1;
                foreach (SBregExUsage s in list)
                {
                    whichRegex.Y++;
                    dict = s.Matches(line);
                    if (dict != null && dict.Count > 0)
                    {
                        setFullBreak = true;
                        break;
                    }
                }
                if (setFullBreak)
                {
                    break;
                }
            }

            if (dict != null && dict.Count > 0)
            {

                //Processing Global Post Parse Functions (Dictionary Modifications)
                foreach (var GlobalPostParseFunc in SBregExHelperFuncs.GlobalPostParseFuncList)
                {
                    dict = GlobalPostParseFunc(dict);
                }

                GlobalVariables.WriteLineToDebugLog("RegEx Match :: " + line);
                this.valid = true;
                this.time = dict["time"];
                this.source = dict["source"];
                this.value_type = dict["type"];
                this.event_type = dict["event_type"];
                this.event_detail = dict["event_detail"];
                this.regExIndx = whichRegex;
                //GlobalVariables.SBSetupHelper.regExUse[whichRegex] = GlobalVariables.SBSetupHelper.regExUse[whichRegex] + 1;

                switch (this.value_type)
                {
                    case "LogFile":
                        switch (this.event_type) 
                        {
                            case "Start":
                                if (ActGlobals.oFormActMain.InCombat == true)
                                { 
                                    GlobalVariables.WriteLineToDebugLog("LogFile Action - Start :: Ending Combat");
                                    ActGlobals.oFormActMain.EndCombat(true);
                                }

                                GlobalVariables.WriteLineToDebugLog("LogFile Action :: Changing Zone to " + this.event_detail);
                                ActGlobals.oFormActMain.ChangeZone(this.event_detail);
                                break;
                            
                            case "Stop":
                                if (ActGlobals.oFormActMain.InCombat == true)
                                {
                                    GlobalVariables.WriteLineToDebugLog("LogFile Action - Stop :: Ending Combat");
                                    ActGlobals.oFormActMain.EndCombat(true);
                                }
                                break;

                            default:
                                GlobalVariables.WriteLineToDebugLog("LogFile Action :: UNKNOWN " + this.event_type + " :: " + this.event_detail);
                                break;
                            
                        }
                        this.valid = false;
                        break;

                    case "cri":
                    case "cry":
                        this.value_type = "cry";
                        this.target = "none";
                        this.value = 0;
                        this.ability = dict["ability"];
                        break;

                    case "active":
                        this.ability = dict["ability"];
                        if (this.ability.Equals("dodg"))
                        {
                            this.ability = "dodge";
                        }
                        this.value = 0;
                        this.target = "none";
                        break;

                    case "kick dirt":
                    case "kicks dirt":
                        this.value_type = "kicks dirt";
                        this.ability = "Knavery (Blind)";
                        this.value = 0;
                        this.target = dict["target"];
                        break;

                    case "surround":
                        this.value = 0;
                        this.target = dict["target"];
                        switch (this.source)
                        {
                            case "black mantle":
                                this.ability = "Shadow Mantle";
                                break;
                            case "protective shield":
                                this.ability = "UNKNOWN";
                                break;
                            default:
                                this.valid = false;
                                break;
                        }
                        break;

                    case "suffer":
                        this.ability = dict["ability"];
                        this.value = GetConvertedValueFromString(dict["value"]);
                        this.target = dict["target"];
                        break;

                    case "take":
                        this.ability = dict["source"];
                        this.value = GetConvertedValueFromString(dict["value"]);
                        this.target = dict["target"];
                        break;

                    case "parr":
                        this.value_type = "parry";
                        this.ability = "melee";
                        this.target = dict["target"];
                        this.value = 0;
                        break;

                    case "dodge":
                    case "block":
                    case "miss":
                        this.ability = "melee";
                        this.target = dict["target"];
                        this.value = 0;

                        if (this.target.Equals("target"))
                        {
                            this.valid = false;
                            this.skipLog = true;
                        }    
                        break;

                    case "hit":
                        this.ability = "melee";
                        this.target = dict["target"];
                        this.value = GetConvertedValueFromString(dict["value"]);
                        break;

                    case "bleed":
                        if (dict["ability"].Equals(""))
                        {
                            this.ability = "bleed";
                        }
                        else
                        {
                            this.ability = dict["ability"];
                        }
                        this.target = dict["target"];
                        this.value = GetConvertedValueFromString(dict["value"]);
                        break;

                    case "slash":
                    case "slashe":
                    case "buffet":
                    case "impale":
                    case "damage":
                    case "blast":
                    case "engulf":
                    case "burn":
                    case "poison":
                    case "freeze":
                    case "drain":
                    case "strike":
                    case "fire":
                    case "electrocute":
                    case "lightning":
                    case "smite":
                    case "shock":
                    case "hurt":
                    case "heal":
                        if (this.source.Equals("") || this.source.Equals("The"))
                        {
                            this.source = "You";
                        }

                        this.ability = dict["ability"];

                        if (this.value_type.Equals("slashe"))
                        {
                            this.value_type = "slash";
                        }

                        switch (this.ability)
                        { 
                            case "is resistant to":
                                this.ability = "resist";
                                this.target = "none";
                                this.value = 0;
                                break;

                            case "blade now glistens with":
                                this.ability = "UNKNOWN";
                                this.source = "UNKNOWN";
                                this.target = "blade";
                                this.value = 0;
                                this.valid = false;
                                break;

                            default:
                                this.target = dict["target"];
                                this.value = GetConvertedValueFromString(dict["value"]);
                                break;
                        }
                        break;

                    case "die":
                        this.ability = "death";
                        this.target = dict["source"];
                        //this.source = "none";
                        this.value = 0;
                        break;

                    case "attack":
                        this.ability = "Challenge";
                        this.target = dict["target"];
                        this.value = 0;
                        break;

                    case "enter":
                    case "execute":
                    case "assume":
                    case "use":
                    case "cast":
                        if (this.event_type.Contains("Powers"))
                        {
                            if (this.event_detail.Contains("Error"))
                            {
                                this.valid = false;
                                this.skipLog = true;
                            }
                        }
                        else
                        {
                            this.ability = dict["ability"];
                            this.target = "none";
                            this.value = 0;

                            if (this.source.Contains("can no longer"))
                            {
                                this.valid = false;
                            }
                        }
                        break;

                    case "returns":
                    case "fades":
                        this.ability = dict["ability"];
                        this.target = dict["source"]; 
                        this.value = 0;
                        this.source = "none";
                        break;
                        
                    default:
                        this.ability = "UNKNOWN"; 
                        this.target = "";
                        this.value = 0;
                        GlobalVariables.WriteLineToUnknownParse("UNKOWN :: Line      :: " + line);
                        GlobalVariables.WriteLineToUnknownParse("UNKOWN :: RegExIndx :: " + this.regExIndx);
                        GlobalVariables.WriteLineToUnknownParse("UNKOWN :: RegExDesc :: " + GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExType + " :: " +
                                                                                            GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExSubType + " :: " +
                                                                                            GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExString);

                        GlobalVariables.WriteLineToUnknownPBLog(line);
                        this.valid = false;
                        break;
                };

                if (!this.skipLog)
                {
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Valid      = " + this.valid.ToString());
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Time       = " + this.time);
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Source     = " + this.source);
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Ability    = " + this.ability);
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Target     = " + this.target);
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Value      = " + this.value.ToString());
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Value_Type = " + this.value_type);
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Event_Type = " + this.event_type);
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: Event_Deta = " + this.event_detail);
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: RegExIndx  = " + this.regExIndx);
                    GlobalVariables.WriteLineToDebugLog("RegEx Match :: RegExDesc  = " + GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExType + " :: " +
                                                                                         GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExSubType + " :: " +
                                                                                         GlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExString); ;
                }
            }

            if (!this.valid && !this.skipLog)
            {     
                GlobalVariables.WriteLineToMissedParse(line);
            }     

        }
    }
}
