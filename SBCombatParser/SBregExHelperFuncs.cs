// Kartas Starfurie
// kstarfurie@yahoo.com

#define PRE_PARSE_LOG_ON
#define POST_PARSE_LOG_ON

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace SBCombatParser
{
    public class SBregExHelperFuncs
    {
        public delegate string SBregExHelper_PreParse(string str);
        public delegate Dictionary<string, string> SBregExHelper_PostParse(Dictionary<string, string> dic);

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
                SBGlobalVariables.WriteLineToPreParseLog("Before : " + original, callingMethod);
                SBGlobalVariables.WriteLineToPreParseLog("After  : " + after, callingMethod);
                SBGlobalVariables.WriteLineToPreParseLog("", callingMethod);
            }
        }

        public static Dictionary<string, string> SBregExHelper_PostParse_Source_ReduceToFirstName(Dictionary<string, string> dict)
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
                //Disc Rune Dropers
                case "Sss'shaass'naal":
                case "Grimtooth":
                case "Marrow Gnawer":
                case "Agrathor the Black":
                case "Kas'ravool the Insidious":
                case "Keelneth Plague-monger":
                case "Tormescu, the Night Fiend":
                case "Grimtalon the Stonefiend":
                case "Aurrek Elf's-bane":
                case "Krah'gool the Crusher":
                case "Guruuk, Chief's Son":
                case "Knellican the Black":
                case "Renegade Thrall Gorkoth":
                case "Lirek, Bhalalar Khar'uus":
                case "Khorvoss Bloodstealer, Khal'akar":
                case "Rath'abroo the Foul":
                case "Carlissa, the Blood Zealot":
                case "Th'verrinax the Banelord":
                case "Esh, Terror of the Sands":
                case "Sss'thalark":
                case "Hrassekht, Lord of Scorpions":
                case "Hoaak Frostfangs":
                case "Canghril, the Blood Knight":
                case "Hurush Ironclaws":
                case "Aelginar the Elflord":
                case "Erleggon the Corpse Stealer":
                case "T'chorrblex the Bloated Terror":
                case "Lord Althannor the Deathless":
                case "Sir Balfour the Blue":
                case "Syarl, the Obsidian Fox":
                case "Halfdan the Black":
                case "Grendohl":
                case "Lord Valtyr the Wulfhednar":
                case "Bolthorn the Rime Drake":
                case "Conn MacMorgan, Hillman Renegade":
                case "Trethellar Plague Spawner":
                case "Mak'toth the Destroyer":
                case "Skaltok the Burner":
                case "Vorrin's Doom":
                case "Hrungar Bear-Lord":
                case "Vagheer the Stalker":
                case "Galkan the Deadly":
                case "Taelmar 'the Spider'":
                case "Mimmur the One-Eyed":
                case "Exiled King Mahn-Tor":
                case "Scirrivax the Blue":
                case "Koss'thrahaa":
                case "Sir Rangorn the Strong":
                case "Hymir the Grim":
                case "Kalthanax, Bane of Cities":
                case "Bok the Uncreated":
                case "Herath the Fell":
                case "Glandor Ingloridan":
                case "Bal'Keth Stoneface":
                case "Acheros the Defiant":
                case "Greeschak, Veshtai Shaman":
                case "Arnakhul, the Blood Beast":
                //NPCs
                case "Vorok Mud-Chief":
                case "Ogre Marauder":
                case "Mud Ogre":
                case "Moss Ogre":
                case "Tavok Chief-brother":
                case "Novac the Bellugh Nuathal Priest":
                case "Naag Warrior":
                case "Naag Prophet":
                case "Naag Guard":
                case "Naag Cultist":
                case "Naag Mystic":
                case "Shaarduk Warrior":
                case "Shaarduk Scout":
                case "Shaarduk Runemaster":
                case "Dread Manticore":
                case "Blizzard Troll":
                case "Frost Bear":
                case "Glyph Drake":
                case "Greater Glyph Drake":
                case "Bone Crusher Scout":
                case "Bone Crusher Axegrinder":
                case "Bone Crusher Hunter":
                case "Gund Earthshaker":
                case "Large Bone Crusher Orc":
                case "Borgak Brainspiller":
                case "Captain Kugaar":
                case "Sand Lizard":
                case "Well Tender":
                //Pets
                case "Vashteera's Companion":
                case "Vashteera's Thrall":
                //Shadow Mantle
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
                SBGlobalVariables.WriteLineToPostParseLog("Compound Name -- Modified :: " + originalStr + " TO " + str);
            }

            return str;
        }

    }
}
