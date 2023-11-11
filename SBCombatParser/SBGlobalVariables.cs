// Kartas Starfurie
// kstarfurie@yahoo.com

#define PRE_PARSE_LOG_ON
#define POST_PARSE_LOG_ON

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Advanced_Combat_Tracker;

namespace SBCombatParser
{
    public class SBGlobalVariables
    {
        static public string SBLogDir = "./SB_Logs";
        static public string logFile = "SBCombatParser.log.txt";
        static public string noParseFile = "SBMissParse.log.txt";
        static public string unkFile = "SBUnkParse.log.txt";
        static public string unkPBFile = "SBUnkPB.log.txt";
        static public string preParseFile = "SBPreParse.log.txt";
        static public string postParseFile = "SBPostParse.log.txt";
        static public string enhanceLineFile = "SBEnhanceLine.log.txt";
        static public string enhanceParseFile = "SBEnhanceParse.log.txt";

        static public string version = "0.0.15.5";
        static public readonly object fileLock = new object();
        static public readonly object missFilelock = new object();
        static public readonly object unkFilelock = new object();
        static public readonly object unkPBFilelock = new object();
        static public readonly object regExFileWriteLock = new object();
        static public readonly object preParseWriteLock = new object();
        static public readonly object postParseWriteLock = new object();
        static public readonly object enhanceLineLock = new object();
        static public readonly object enhanceLineParseLock = new object();

        static public SBSetupHelper setupHelper = new SBSetupHelper();

        static public Dictionary<string, SBThreadLogger> sbThreadLoggers = new Dictionary<string, SBThreadLogger>();

        //static private bool threadLoggerSetupRun = false;
        

        static public void SetupThreadLoggers() 
        {
            sbThreadLoggers.Clear();
#if DEBUG
            sbThreadLoggers.Add("DebugLog", new SBThreadLogger(SBLogDir + "/" + logFile));
            sbThreadLoggers.Add("PreParseLog", new SBThreadLogger(SBLogDir + "/" + preParseFile));
            sbThreadLoggers.Add("PostParseLog", new SBThreadLogger(SBLogDir + "/" + postParseFile));
            sbThreadLoggers.Add("RegExFile", new SBThreadLogger());
            sbThreadLoggers.Add("MissedParse", new SBThreadLogger(SBLogDir + "/" + noParseFile));
            sbThreadLoggers.Add("UnknownParse", new SBThreadLogger(SBLogDir + "/" + unkFile));
            sbThreadLoggers.Add("UnknownPBLog", new SBThreadLogger(SBLogDir + "/" + unkPBFile));
            sbThreadLoggers.Add("EnhanceLineLog", new SBThreadLogger(SBLogDir + "/" + enhanceLineFile));
            sbThreadLoggers.Add("EnhanceParseLog", new SBThreadLogger(SBLogDir + "/" + enhanceParseFile));
#endif            
            sbThreadLoggers.Add("TTS", new SBThreadLogger("TTS"));
            //SBGlobalVariables.threadLoggerSetupRun = true;
        }

        static public void TearDownThreadLoggers()
        {
            foreach(KeyValuePair<string, SBThreadLogger> kvp in sbThreadLoggers)
            {
                kvp.Value.threadCancel.Cancel();
            }

            foreach (KeyValuePair<string, SBThreadLogger> kvp in sbThreadLoggers)
            {
                kvp.Value.Join();
            }

        }

        static public void WriteLineToTTS(string line)
        {
            sbThreadLoggers["TTS"].fifoQueue.Add(new SBPutToLog(line));
        }

        static public void WriteLineToDebugLog(string line, [CallerMemberName] string callingMethod = "")
        {
#if DEBUG
            sbThreadLoggers["DebugLog"].fifoQueue.Add(new SBPutToLog(callingMethod + " :: " + line));
            
#endif
        }
        static public void WriteLineToPreParseLog(string line, [CallerMemberName] string callingMethod = "")
        {
#if DEBUG
#if PRE_PARSE_LOG_ON
            sbThreadLoggers["PreParseLog"].fifoQueue.Add(new SBPutToLog(callingMethod + " :: " + line));
#endif
#endif
        }
        static public void WriteLineToPostParseLog(string line, [CallerMemberName] string callingMethod = "")
        {
#if DEBUG
#if POST_PARSE_LOG_ON
            sbThreadLoggers["PostParseLog"].fifoQueue.Add(new SBPutToLog(callingMethod + " :: " + line));
#endif
#endif
        }
        static public void WriteLineToRegExFile(string filename, string line)
        {
#if DEBUG
            sbThreadLoggers["RegExFile"].fifoQueue.Add(new SBPutToLog(line, SBLogDir + "/" + filename));
#endif
        }
        static public void WriteLineToMissedParse(string line)
        {
#if DEBUG
            sbThreadLoggers["MissedParse"].fifoQueue.Add(new SBPutToLog(line));
#endif
        }
        static public void WriteLineToUnknownParse(string line)
        {
#if DEBUG
            sbThreadLoggers["UnknownParse"].fifoQueue.Add(new SBPutToLog(line));
#endif
        }
        static public void WriteLineToUnknownPBLog(string line)
        {
#if DEBUG
            sbThreadLoggers["UnknownPBLog"].fifoQueue.Add(new SBPutToLog(line));
#endif
        }
        static public void WriteLineToEnchanceLineLog(string line)
        {
#if DEBUG
            sbThreadLoggers["EnhanceLineLog"].fifoQueue.Add(new SBPutToLog(line));
#endif
        }
        static public void WriteLineToEnhanceParseLog(string line)
        {
#if DEBUG
            sbThreadLoggers["EnhanceParseLog"].fifoQueue.Add(new SBPutToLog(line));
#endif
        }

        public class SBPutToLog
        {
            public string filename;
            public string line;

            public SBPutToLog(string line, string filename = null)
            {
                this.line = line;
                this.filename = filename;
            } 
        }

        public class SBThreadLogger
        {
            private String filename;
            private Thread thread;
            public BlockingCollection <SBPutToLog> fifoQueue;
            private Object fileLock;
            //public bool threadStop;
            public CancellationTokenSource threadCancel;

            public SBThreadLogger(string filename = null)
            {
                //this.threadStop = false;
                this.threadCancel = new CancellationTokenSource();
                this.filename = filename;
                this.fileLock = new Object();
                this.fifoQueue = new BlockingCollection<SBPutToLog>();
                this.thread = new Thread(() => this.ProcessQueue());

                this.thread.Start();
            }

            public void Join()
            {
                if (!this.thread.Join(15))
                {
                    //This isn't recommeneded ... but otherwise ACT kind of hangs for a few seconds on exit.
                    this.thread.Abort();
                }
            }

            private void ProcessQueue()
            {
                while (!this.threadCancel.IsCancellationRequested)
                {
                    foreach(SBPutToLog logIt in fifoQueue.GetConsumingEnumerable())
                    {
                        this.WriteToFile(logIt.line, logIt.filename);
                    }
                    Thread.Sleep(10);
                }
            }

            private void WriteToFile(string line, string filename = null)
            {
                if(filename == null)
                {
                    filename = this.filename;
                }

                if (filename.Equals("TTS"))
                {
                    ActGlobals.oFormActMain.TTS(line);
                }
                else
                {
                    lock (this.fileLock)
                    {
                        //SBGlobalVariables.SBLogDir + "/" + 
                        using (StreamWriter writer = File.AppendText(filename))
                        {
                            writer.AutoFlush = true;
                            writer.WriteLine(line);
                        }
                    }
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
                normalPriorityRegEx.Add(new SBregExUsage("Heal", "Apos", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

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

                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*) (?<type>drain)s (?<value>\d*) points of (?<event_type>[helthmansi]*) from (?<target>.*) with (?<ability>.*)[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Drain", "Val First", myRegEx, RegexOptions.Compiled, writeALLlogFiles | true));

                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']?[s]? (?<ability>.*) (?<type>.*)s (?<value>\d*) points of (?<event_type>[helthman]*) from (?<target>.*)[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Drain", "Val Last", myRegEx, RegexOptions.Compiled, writeALLlogFiles | true));

                //Backstab drains
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) (?<type>\w*?)[e]?[s]? (?<value>\d*) points of (?<event_type>.*?)\W?damage from (?<source>.*?)[r']+[s]? (?<ability>.*)[\.!]";
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

                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']{1}[s]* (?<ability>.*?) (?<type>\w{3,11}?)s (?<target>.*) for (?<value>\d*) points of (?<event_type>.*) damage[\.!]";
                normalPriorityRegEx.Add(new SBregExUsage("Damage", "Spell mana or stamina", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

                //Matches hurt, shock, smite, heal
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?)[r']{1}[s]* (?<ability>.*?) (?<type>\w{3,11}?)s (?<target>.*) for (?<value>\d*) .*[\.!]";
                sbreu = new SBregExUsage("Damage", "Spell", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Source_ReduceToFirstName);
                //sbreu.regPostParseFuncList.Add(SBregExHelperFuncs.SBregExHelper_PostParse_Target_ReduceToFirstName);
                normalPriorityRegEx.Add(sbreu);

                //bleeds
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*) (?<type>bleed)[s]? for (?<value>\d*) points.*[\.!]";
                sbreu = new SBregExUsage("Damage", "Bleed No Take", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                normalPriorityRegEx.Add(sbreu);

                //Burn You
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*?) is (?<type>burn)[ed]* for (?<value>\d*) points.*[\.!]";
                sbreu = new SBregExUsage("Damage", "Burn You", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                normalPriorityRegEx.Add(sbreu);

                //Proc Damage - You
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*?) (?<ability>.*?) (?<type>\w*)s (?<target>.*) for (?<value>\d*) .*[\.!]";
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
                //myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*) (?<type>kick[s]? dirt) .* (?<target>\w*)[r's]+ .*[\.!]";
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<source>.*) (?<type>kick[s]? dirt) .* (?<target>\w*)[r's]* .*[\.!]";
                sbreu = new SBregExUsage("Debuff", "Blind", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
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

                //Use Power -- can no longer use
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*?) (?<event_type>can no longer) (?<type>use)s? [\Wa]?(?<ability>.*)[\.!]";
                lowPriorityRegEx.Add(new SBregExUsage("Power", "Use no longer", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false));

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

                //Start Bleeding
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*?) (?<event_type>\w*?)[s]? (?<type>bleed)[ing]*[\.!]";
                sbreu = new SBregExUsage("Debuff", "Bleed-start", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                lowPriorityRegEx.Add(sbreu);

                //Exposed
                myRegEx = @"\((?<time>\d*\:\d*\:\d*)\)\W*(?<target>.*?) is (?<type>expose).* to [a]?\W?(?<event_type>.*).*[\.!]";
                sbreu = new SBregExUsage("Expose", "expose", myRegEx, RegexOptions.Compiled, writeALLlogFiles | false);
                lowPriorityRegEx.Add(sbreu);




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
}
