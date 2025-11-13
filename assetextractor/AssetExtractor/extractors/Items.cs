using System;
using System.IO;
using RoR2;
using RoR2.ContentManagement;
using Path = System.IO.Path;

namespace AssetExtractor;

public static partial class WikiFormat
{
    public static void FormatItem(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_ITEM);
        var f =
            "items[\"{0}\"] = {{\n\tRarity = \"{1}\",\n\tQuote = \"{2}\",\n\tDesc = \"{3}\",\n\tCategory = {{ {4} }},\n\tUnlock = \"{5}\",\n\tCorrupt = \"{6}\", \n\tUncorrupt = \"{7}\",\n\tID = ,\n\tStats = {{\n\t\t {{\n\t\t\tStat = \"\",\n\t\t\tValue = \"\",\n\t\t\tStack = \"\",\n\t\t\tAdd = \"\"\n\t\t}}\n\t}},\n\tLocalizationInternalName = \"{8}\",\n\t}}";
        if (!Directory.Exists(WikiOutputPath)) Directory.CreateDirectory(WikiOutputPath);

        TextWriter tw = new StreamWriter(path, WikiAppend);

        foreach (var def in readOnlyContentPack.itemDefs) ItemDefFormat(def, tw, f);

        void ItemDefFormat(ItemDef def, TextWriter tw, string f)
        {
            try
            {
                var item = def;
                var itemName = "";
                var pickup = "";
                var desc = "";
                var tags = "";
                var token = "";

                if (item == null) return;

                if (item.nameToken != null)
                {
                    itemName = Language.GetString(item.nameToken);

                    if (Language.english.TokenIsRegistered(item.nameToken.Replace("_NAME", "_LORE")))
                        loredefs.Add("Items " + item.nameToken + " " + item.nameToken.Replace("_NAME", "_LORE"));
                }

                var itemTier = item.tier;
                var rarity = itemTier switch
                {
                    ItemTier.Tier1 => "Common",
                    ItemTier.Tier2 => "Uncommon",
                    ItemTier.Tier3 => "Legendary",
                    ItemTier.Lunar => "Lunar",
                    ItemTier.Boss => "Boss",
                    ItemTier.VoidTier1 => "Void",
                    ItemTier.VoidTier2 => "Void",
                    ItemTier.VoidTier3 => "Void",
                    ItemTier.VoidBoss => "Void",
                    _ => "Untiered"
                };

                if (item.pickupToken != null) pickup = Language.GetString(item.pickupToken);

                if (item.descriptionToken != null) desc = Language.GetString(item.descriptionToken);


                for (var i = 0; i < item.tags.Length; i++)
                {
                    tags += "\"" + Enum.GetName(typeof(ItemTag), item.tags[i]) + "\"";
                    if (i < item.tags.Length - 1) tags += ",";
                }

                var unlock = "";
                if (item.unlockableDef)
                {
                    var achievement =
                        AchievementManager.GetAchievementDefFromUnlockable(item.unlockableDef.cachedName);
                    if (achievement != null && !string.IsNullOrEmpty(achievement.nameToken))
                        unlock = Language.GetString(achievement.nameToken);
                }

                if (item.nameToken != null && item.nameToken.EndsWith("_NAME"))
                    token = item.nameToken.Remove(item.nameToken.Length - 5); // remove _NAME
                else if (item.nameToken != null) token = item.nameToken;

                var format = Language.GetStringFormatted(f, itemName, rarity, pickup, desc, tags, unlock, string.Empty,
                    string.Empty, token);
                foreach (var kvp in FormatR2ToWiki) format = format.Replace(kvp.Key, kvp.Value);

                tw.WriteLine(format);

                if (!item.pickupIconTexture) return;
                var temp = WikiOutputPath + @"\items\";

                Directory.CreateDirectory(temp);
                try
                {
                    if (itemName == "")
                    {
                        Log.Debug("item name is blank ! using toke n");
                        exportTexture(item.pickupIconSprite.texture, Path.Combine(temp, token + WikiModname + ".png"));
                    }
                    else
                    {
                        exportTexture(item.pickupIconSprite.texture,
                            Path.Combine(temp, itemName.Replace(" ", "_") + WikiModname + ".png"));
                    }
                }
                catch (Exception e)
                {
                    Log.Debug(e);
                    Log.Debug("erm ,,.,. failed to export equip icon with proper name ,,. trying with tokenm !! " +
                              itemName);
                    exportTexture(item.pickupIconSprite.texture, Path.Combine(temp, token + WikiModname + ".png"));
                }
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting item: " + e);
            }
        }

        tw.Close();

        var length = new FileInfo(path).Length;
        if (length <= 0) File.Delete(path);
    }
}