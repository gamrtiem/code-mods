using System;
using System.IO;
using RoR2.ContentManagement;
using UnityEngine;
using Path = System.IO.Path;

namespace AssetExtractor;

public partial class WikiFormat
{
    public static void FormatBuffs(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_BUFFS);

        var f = "StatusEffects[\"{0}\"] = {{\n";
        f += "\tName = \"{1}\",\n";
        f += "\tInternalName = \"{2}\",\n";
        f += "\tImage = \"{3}\",\n";
        f += "\tEffectShort = \"\",\n";
        f += "\tEffect = \"\",\n";
        f += "\tSource = {{}},\n";
        f += "\tType = \"{4}\",\n"; // buff or affix buff or debuff or cooldown buff
        f += "\tStackable = \"{5}\",\n";
        f += "\tDOT = \"{6}\",\n";
        f += "\tColor = \"{7}\",\n";
        f += "\tHidden = \"{8}\",\n";
        f += "\t}}";

        if (!Directory.Exists(WikiOutputPath)) Directory.CreateDirectory(WikiOutputPath);
        TextWriter tw = new StreamWriter(path, WikiAppend);

        foreach (var buffdef in readOnlyContentPack.buffDefs)
            try
            {
                string type;
                var image = "Status ";

                if (buffdef == null) continue;

                if (buffdef.isDebuff)
                    type = "Debuff";
                else if (buffdef.isElite)
                    type = "Affix Buff";
                else if (buffdef.isCooldown)
                    type = "Cooldown Buff";
                else
                    type = "Buff";

                var dot = buffdef.isDOT ? "True" : "False";
                var stackable = buffdef.canStack ? "True" : "False";
                var hidden = buffdef.isHidden ? "True" : "False";
                var color = $"#{ColorUtility.ToHtmlStringRGB(buffdef.buffColor)}";
                var name = buffdef.name;
                if (name.StartsWith("bd")) name = name[2..];
                image += name;

                if (buffdef.iconSprite)
                {
                    var temp = WikiOutputPath + @"\buffs\";
                    Directory.CreateDirectory(temp);
                    try
                    {
                        exportTexture(buffdef.iconSprite, Path.Combine(temp, "Status " + name.Replace(" ", "_") + WikiModname + ".png"));
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"erm ,,.,. failed to export buff icon {buffdef.name} ,,. {e}");
                    }
                }

                var format = string.Format(f, name, name, name, image, type, stackable, dot, color, hidden);

                foreach (var kvp in FormatR2ToWiki) format = format.Replace(kvp.Key, kvp.Value);

                tw.WriteLine(format);
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting buff: " + e);
            }

        tw.Close();

        var length = new FileInfo(path).Length;
        if (length <= 0) File.Delete(path);
    }
}