// Kartas Starfurie
// kstarfurie@yahoo.com

#define PRE_PARSE_LOG_ON
#define POST_PARSE_LOG_ON

using System;
using System.Collections.Generic;
using Advanced_Combat_Tracker;
using System.Drawing;

namespace SBCombatParser
{
    public class SBLogLine
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
        public Point regExIndx = new Point(-1, -1);
        public int globalTimeSorter = 0;
        public string enh_resist_type = string.Empty;

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
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Message    = " + ex.Message);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: dict[\"value\"] = " + str);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Valid      = " + this.valid.ToString());
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Time       = " + this.time);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Source     = " + this.source);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Ability    = " + this.ability);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Target     = " + this.target);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Value      = " + retVal.ToString());
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Value_Type = " + this.value_type);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Event_Type = " + this.event_type);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: Event_Deta = " + this.event_detail);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: RegExIndx  = " + this.regExIndx);
                SBGlobalVariables.WriteLineToDebugLog("FormatException :: RegExDesc  = " + SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExType + " :: " +
                                                                                         SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExSubType + " :: " +
                                                                                         SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExString);

                retVal = 0;
            }

            return retVal;
        }

        public SBLogLine(string line, int actGTS)
        {




            this.globalTimeSorter = actGTS;

            //Processing Global Pre Parse Functions (string Modifications)
            foreach (var GlobalPreParseFunc in SBregExHelperFuncs.GlobalPreParseFuncList)
            {
                line = GlobalPreParseFunc(line);
            }

            SBGlobalVariables.WriteLineToDebugLog(line);

            this.valid = false;

            //line = id_regex.Replace(line, "");
            //MatchCollection matchesHeal = regexHeals.Matches(line);
            Dictionary<string, string> dict = null;
            bool setFullBreak = false;
            //int whichRegex = -1;
            Point whichRegex = new Point(-1, -1);
            foreach (List<SBregExUsage> list in SBGlobalVariables.SBSetupHelper.allRegEx)
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

                SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: " + line);
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
                                    SBGlobalVariables.WriteLineToDebugLog("LogFile Action - Start :: Ending Combat");
                                    ActGlobals.oFormActMain.EndCombat(true);
                                }

                                SBGlobalVariables.WriteLineToDebugLog("LogFile Action :: Changing Zone to " + this.event_detail);
                                ActGlobals.oFormActMain.ChangeZone(this.event_detail);
                                break;

                            case "Stop":
                                if (ActGlobals.oFormActMain.InCombat == true)
                                {
                                    SBGlobalVariables.WriteLineToDebugLog("LogFile Action - Stop :: Ending Combat");
                                    ActGlobals.oFormActMain.EndCombat(true);
                                }
                                break;

                            default:
                                SBGlobalVariables.WriteLineToDebugLog("LogFile Action :: UNKNOWN " + this.event_type + " :: " + this.event_detail);
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
                        this.ability = "Knavery";
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
                        if (this.source.Equals(""))
                        {
                            this.source = "none";
                        }
                        if (dict["ability"].Equals(""))
                        {
                            this.ability = "bleed";
                        }
                        else
                        {
                            this.ability = dict["ability"];
                        }
                        this.target = dict["target"];
                        if(!(this.event_type.Equals("start") | this.event_type.Equals("stop")))
                        {
                            this.value = GetConvertedValueFromString(dict["value"]);
                        }
                        else
                        {
                            this.value = 0;
                        }
                        break;
                    case "expose":
                        if(this.source.Equals(""))
                        {
                            this.source = "none";
                        }
                        this.target = dict["target"];
                        this.value = 0;
                        break;
                    case "tak":
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
                            this.source = "none";
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
                        else if (this.event_type.Contains("can no longer") && this.ability.Contains("power"))
                        {
                            this.ability = "Power Block";
                            this.target = dict["target"];
                            this.source = "none";
                            this.value = 0;
                        }
                        else
                        {
                            this.ability = dict["ability"];
                            this.target = "none";
                            this.value = 0;
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
                        SBGlobalVariables.WriteLineToUnknownParse("UNKOWN :: Line      :: " + line);
                        SBGlobalVariables.WriteLineToUnknownParse("UNKOWN :: RegExIndx :: " + this.regExIndx);
                        SBGlobalVariables.WriteLineToUnknownParse("UNKOWN :: RegExDesc :: " + SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExType + " :: " +
                                                                                            SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExSubType + " :: " +
                                                                                            SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExString);

                        SBGlobalVariables.WriteLineToUnknownPBLog(line);
                        this.valid = false;
                        break;
                };

                if (!this.skipLog)
                {
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Valid      = " + this.valid.ToString());
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Time       = " + this.time);
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Source     = " + this.source);
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Ability    = " + this.ability);
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Target     = " + this.target);
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Value      = " + this.value.ToString());
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Value_Type = " + this.value_type);
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Event_Type = " + this.event_type);
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: Event_Deta = " + this.event_detail);
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: RegExIndx  = " + this.regExIndx);
                    SBGlobalVariables.WriteLineToDebugLog("RegEx Match :: RegExDesc  = " + SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExType + " :: " +
                                                                                         SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExSubType + " :: " +
                                                                                         SBGlobalVariables.SBSetupHelper.allRegEx[this.regExIndx.X][this.regExIndx.Y].regExString); ;
                }
            }

            if (!this.valid && !this.skipLog)
            {
                SBGlobalVariables.WriteLineToMissedParse(line);
            }
        }

        public static SBLogLine EnhanceLineValues(SBLogLine line)
        {
            line.enh_resist_type = "Unknown";
            switch (line.value_type)
            {
                case "expose":
                    switch (line.event_type)
                    {
                        case "slashing attacks":
                        case "slashing attack":
                            line.enh_resist_type = "Slash";
                            break;
                        case "pinpoint strike":
                            line.enh_resist_type = "Pierce";
                            break;
                        case "bludgeon":
                        case "well-placed bludgeon attack":
                            line.enh_resist_type = "Crush";
                            break;
                        default:
                            line.event_type = line.event_type + "-Unknown";
                            break;
                    }
                    break;
                case "assume":
                    line.enh_resist_type = "Stance";
                    break;
                case "bleed":
                    line.enh_resist_type = "Bleed";
                    break;
                case "execute":
                    line.enh_resist_type = "Power-Execute";
                    break;
                case "cry":
                case "use":
                case "cast":
                    line.enh_resist_type = "Ability-Cast";
                    break;
                case "heal":
                    line.enh_resist_type = "Heal";
                    break;
                case "hit":
                    switch (line.ability)
                    {
                        case "melee":
                            line.enh_resist_type = "C/P/S";
                            break;
                        default:
                            line.enh_resist_type = "Unknown hit";
                            break;
                    }
                    break;
                case "buffet":
                    line.enh_resist_type = "Crush";
                    break;
                case "engulf":
                    line.enh_resist_type = "Unholy";
                    break;
                case "poison":
                    line.enh_resist_type = "Poison";
                    break;
                case "freeze":
                    line.enh_resist_type = "Cold";
                    break;
                case "smite":
                    line.enh_resist_type = "Holy";
                    break;
                case "blast":
                case "burn":
                case "fire":
                    line.enh_resist_type = "Fire";
                    break;
                case "lightning":
                case "electrocute":
                case "shock":
                    line.enh_resist_type = "Lightning";
                    break;
                case "hurt":
                    switch (line.ability)
                    {
                        case "Blood Boil":
                            line.enh_resist_type = "Fire";
                            break;
                        case "Earthquake":
                            line.enh_resist_type = "Crush";
                            break;
                        case "Mark of The All-Father":
                            line.enh_resist_type = "Holy";
                            break;
                        case "Psychic Shout":
                        case "Mind Strike":
                            line.enh_resist_type = "Mental";
                            break;
                        case "Mystic Backlash":
                        case "Pallando's Pernicious Puns":
                        case "Sign of Sorthoth":
                        case "Dread Dissonance":
                            line.enh_resist_type = "Magic";
                            break;
                        default:
                            line.enh_resist_type = "Hurt-Unknown";
                            break;
                    }
                    break;

                case "impale":
                    line.enh_resist_type = "Pierce";
                    break;
                case "slash":
                case "slashe":
                    line.enh_resist_type = "Slash";
                    break;
                case "tak":
                case "take":
                    switch (line.ability)
                    {
                        case "bleeding":
                            line.enh_resist_type = "Bleed";
                            break;
                        default:
                            line.enh_resist_type = "Take-Unknown";
                            break;
                    }
                    break;
                case "damage":
                    line.enh_resist_type = "Unknown";
                    break;
                case "drain":
                    line.enh_resist_type = "Drain";
                    break;
                case "strike":
                    switch (line.ability)
                    {
                        case "Unholy Blast":
                        case "Unholy Storm":
                            line.enh_resist_type = "Unholy";
                            break;
                        case "magical burst":
                        case "Mage Bolt":
                            line.enh_resist_type = "Magic";
                            break;
                        case "holy weapon":
                            line.enh_resist_type = "Holy";
                            break;
                        case "poisonous weapon":
                            line.enh_resist_type = "Poison";
                            break;
                        default:
                            line.enh_resist_type = "Strike-Unknown";
                            break;
                    }
                    break;
                case "suffer":
                    switch (line.ability)
                    {
                        case "poison":
                        case "Pellegorn poison":
                        case "Magusbane poison":
                            line.enh_resist_type = "Poison";
                            break;
                        case "Buchinine poison":
                            line.enh_resist_type = "Disease";
                            break;
                        default:
                            line.enh_resist_type= "Suffer-Unknown";
                            break;
                    }
                    break;
                case "kicks dirt":
                    line.enh_resist_type = "Blind";
                    break;
                case "surround":
                    line.enh_resist_type = "None";
                    break;
                default:
                    line.enh_resist_type = "Default-Unknown";
                    break;
            }

            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Valid      = " + line.valid.ToString());
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Time       = " + line.time);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Source     = " + line.source);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Ability    = " + line.ability);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Target     = " + line.target);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Value      = " + line.value.ToString());
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Value_Type = " + line.value_type);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Event_Type = " + line.event_type);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Event_Deta = " + line.event_detail);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: Enh Resist = " + line.enh_resist_type);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: RegExIndx  = " + line.regExIndx);
            SBGlobalVariables.WriteLineToEnchanceLineLog("Enhance Line :: RegExDesc  = " + SBGlobalVariables.SBSetupHelper.allRegEx[line.regExIndx.X][line.regExIndx.Y].regExType + " :: " +
                                                                                 SBGlobalVariables.SBSetupHelper.allRegEx[line.regExIndx.X][line.regExIndx.Y].regExSubType + " :: " +
                                                                                 SBGlobalVariables.SBSetupHelper.allRegEx[line.regExIndx.X][line.regExIndx.Y].regExString);

            
            SBGlobalVariables.WriteLineToEnhanceParseLog($"{line.time,-8} : {line.value_type,-10} : {line.ability,-35} : {line.enh_resist_type,-20} : {line.source,-25} : {line.target,-25} : {line.event_type,-12} : {line.event_detail,-12} : {line.value.ToString(),-6}");


            return line;
        }
    }
}
