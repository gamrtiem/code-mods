using System;
using System.IO;
using RoR2;
using RoR2.ContentManagement;
using Path = System.IO.Path;

namespace AssetExtractor;

public static partial class WikiFormat
{
    public static void FormatLore(ReadOnlyContentPack readOnlyContentPack)
    {
        var path = Path.Combine(WikiOutputPath, WIKI_OUTPUT_LORE);

        var f = "lore[\"{0}\"] = {{\n";
        f += "\tType = \"{1}\",\n";
        f += "\tDesc = \"{2}\",\n";
        f += "\t}}";

        if (!Directory.Exists(WikiOutputPath)) Directory.CreateDirectory(WikiOutputPath);
        TextWriter tw = new StreamWriter(path, WikiAppend);

        foreach (var lore in loredefs)
            try
            {
                var loresplit = lore.Split(" ");

                var format = string.Format(f, loresplit[0], Language.GetString(loresplit[1]),
                    Language.GetString(loresplit[2]).Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t").Replace("\"", "\\\""));

                foreach (var kvp in FormatR2ToWiki) format = format.Replace(kvp.Key, kvp.Value);

                tw.WriteLine(format);
            }
            catch (Exception e)
            {
                Log.Error("Error while exporting lore: " + e);
            }

        tw.Close();

        var length = new FileInfo(path).Length;
        if (length <= 0) File.Delete(path);
    }
}