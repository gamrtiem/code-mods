using System;
using System.IO;
using RoR2;
using RoR2.ContentManagement;
using Path = System.IO.Path;

namespace AssetExtractor;

public static partial class WikiFormat
{
    public static void FormatEquipment(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_EQUIPMENT);
        var f = "equipments[\"{0}\"] = {{\n\tRarity = \"{1}\",\n\tQuote = \"{2}\",\n\tDesc = \"{3}\",\n\tUnlock = \"{4}\",\n\t ID = ,\n\tLocalizationInternalName = \"{5}\",\n\t}}";

        TextWriter tw = new StreamWriter(path, WikiAppend);

        static void FormatEquipmentDef(EquipmentDef def, TextWriter tw, string f)
        {
            try
            {
                var equip = def;

                if (equip == null) return; // you never know 

                var itemName = "";
                var isLunar = equip.isLunar; // this should be fine and if it isnt ill cry 
                var rarity = isLunar ? "Lunar Equipment" : "Equipment";
                var pickup = "";
                var desc = "";
                var unlock = "";
                var token = "";

                if (equip.nameToken != null)
                {
                    itemName = Language.GetString(equip.nameToken);

                    if (Language.english.TokenIsRegistered(equip.nameToken.Replace("_NAME", "_LORE")))
                        loredefs.Add("Equipment " + equip.nameToken + " " + equip.nameToken.Replace("_NAME", "_LORE"));

                    if (equip.nameToken != null && equip.nameToken.EndsWith("_NAME"))
                        token = equip.nameToken.Remove(equip.nameToken.Length - 5); // remove _NAME
                    else if (equip.nameToken != null) token = equip.nameToken;
                }

                if (equip.pickupToken != null) pickup = Language.GetString(equip.pickupToken);

                if (equip.descriptionToken != null) desc = Language.GetString(equip.descriptionToken);

                if (equip.unlockableDef)
                {
                    var nameToken = AchievementManager.GetAchievementDefFromUnlockable(equip.unlockableDef.cachedName)
                        ?.nameToken;
                    if (nameToken != null)
                        unlock = Language.GetString(nameToken);
                }

                var format = Language.GetStringFormatted(f, itemName, rarity, pickup, desc, unlock, token);

                foreach (var kvp in FormatR2ToWiki) format = format.Replace(kvp.Key, kvp.Value);
                tw.WriteLine(format);

                if (!equip.pickupIconTexture) return;

                var temp = WikiOutputPath + @"\equips\";
                Directory.CreateDirectory(temp);
                try
                {
                    if (itemName == "")
                    {
                        Log.Debug("equip name is blank ! using toke n");
                        exportTexture(equip.pickupIconSprite.texture, Path.Combine(temp, token + WikiModname + ".png"));
                    }
                    else
                    {
                        exportTexture(equip.pickupIconSprite.texture,
                            Path.Combine(temp, itemName.Replace(" ", "_") + WikiModname + ".png"));
                    }
                }
                catch
                {
                    Log.Debug("erm ,,.,. failed to export equip icon with proper name ,,. trying with tokenm !! " +
                              itemName);
                    exportTexture(equip.pickupIconSprite.texture, Path.Combine(temp, token + WikiModname + ".png"));
                }
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting equipment: " + e);
            }
        }

        foreach (var def in readOnlyContentPack.equipmentDefs) FormatEquipmentDef(def, tw, f);
        tw.Close();

        var length = new FileInfo(path).Length;
        if (length <= 0) File.Delete(path);
    }
}