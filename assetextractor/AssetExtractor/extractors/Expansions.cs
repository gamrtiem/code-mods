using System;
using System.IO;
using RoR2;
using RoR2.ContentManagement;
using Path = System.IO.Path;

namespace AssetExtractor;

public partial class WikiFormat
{
    public static void exportExpansions(ReadOnlyContentPack readOnlyContentPack)
    {
        foreach (var expansionDef in readOnlyContentPack.expansionDefs)
        {
            string temp = WikiOutputPath + @"\expansions\";
            Directory.CreateDirectory(temp);
            
            try
            {
                if (expansionDef.iconSprite != null)
                {
                    Log.Debug("item name is blank ! using toke n");
                    exportTexture(expansionDef.iconSprite, Path.Combine(temp, Language.GetString(expansionDef.nameToken) + ".png"));
                }
                else
                {
                    Log.Debug("expansion icon is nul l!!!!!!!!!!");
                }
            }
            catch (Exception e)
            {
                Log.Debug(e);
                Log.Debug("erm ,,.,. failed to export expansion icon with proper name ,,. trying with tokenm !! " + Language.GetString(expansionDef.nameToken));
                exportTexture(expansionDef.iconSprite, Path.Combine(temp, expansionDef.nameToken + ".png"));
            }
        }
    
    }
}