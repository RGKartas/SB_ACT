// Kartas Starfurie
// kstarfurie@yahoo.com

#define PRE_PARSE_LOG_ON
#define POST_PARSE_LOG_ON

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SBCombatParser
{
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
            this.regExSubType = regExSubType;
            this.regExString = regExString;
            this.regExOptions = regExOptions;
            this.logIt = logIt;
            if (regLogFileName == null)
            {
                this.regLogFileName = "SB_REG_" + this.regExType.Replace(' ', '_') + "_" + this.regExSubType.Replace(' ', '_') + ".log.txt";
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
            if (this.logIt)
            {
                DateTime currentDateTime = DateTime.Now;
                SBGlobalVariables.WriteLineToRegExFile(this.regLogFileName, "Version     :: " + SBGlobalVariables.version);
                SBGlobalVariables.WriteLineToRegExFile(this.regLogFileName, "DateTime    :: " + currentDateTime);
                SBGlobalVariables.WriteLineToRegExFile(this.regLogFileName, "RegExType   :: " + this.regExType + " :: " + this.regExSubType);
                SBGlobalVariables.WriteLineToRegExFile(this.regLogFileName, "RegExString :: " + this.regExString);
                SBGlobalVariables.WriteLineToRegExFile(this.regLogFileName, "");
                SBGlobalVariables.WriteLineToRegExFile(this.regLogFileName, "");


                SBGlobalVariables.WriteLineToRegExFile(this.regParseFileName, "Version     :: " + SBGlobalVariables.version);
                SBGlobalVariables.WriteLineToRegExFile(this.regParseFileName, "DateTime    :: " + currentDateTime);
                SBGlobalVariables.WriteLineToRegExFile(this.regParseFileName, "RegExType   :: " + this.regExType + " :: " + this.regExSubType);
                SBGlobalVariables.WriteLineToRegExFile(this.regParseFileName, "RegExString :: " + this.regExString);
                SBGlobalVariables.WriteLineToRegExFile(this.regParseFileName, "");
                SBGlobalVariables.WriteLineToRegExFile(this.regParseFileName, $"{"Time",-8} : {"Value_Type",-10} : {"Source",-25} : {"Target",-25} : {"Ability",-25} : {"Event_type",-12} : {"Event_detail",-12} : {"Value",-6}");
            }
        }

        public Dictionary<string, string> Matches(string line)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            foreach (var preProcessFunc in this.regPreParseFuncList)
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
                    SBGlobalVariables.WriteLineToRegExFile(this.regLogFileName, line);
                    SBGlobalVariables.WriteLineToRegExFile(this.regParseFileName, $"{result["time"],-8} : {result["type"],-10} : {result["source"],-25} : {result["target"],-25} : {result["ability"],-25} : {result["event_type"],-12} : {result["event_detail"],-12} : {result["value"],-6}");
                }

                foreach (var postProcessFunc in this.regPostParseFuncList)
                {
                    result = postProcessFunc(result);
                }

            }

            return result;
        }
    }
}
