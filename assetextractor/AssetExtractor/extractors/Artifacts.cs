using System;
using System.IO;
using RoR2;
using RoR2.ContentManagement;
using Path = System.IO.Path;

namespace AssetExtractor;

public partial class WikiFormat
{
    public static void exportArtifacts(ReadOnlyContentPack readOnlyContentPack)
    {
        foreach (var artifactDef in readOnlyContentPack.artifactDefs)
        {
            string temp = WikiOutputPath + @"\artifacts\";
            Directory.CreateDirectory(temp);
            
            try
            {
                if (artifactDef.smallIconSelectedSprite != null)
                {
                    Log.Debug("item name is blank ! using toke n");
                    exportTexture(artifactDef.smallIconSelectedSprite, Path.Combine(temp, Language.GetString(artifactDef.nameToken) + " Selected.png"));
                    exportTexture(artifactDef.smallIconDeselectedSprite, Path.Combine(temp, Language.GetString(artifactDef.nameToken) + " Deselected.png"));
                }
                else
                {
                    Log.Debug("artifact icon is nul l!!!!!!!!!!");
                }
            }
            catch (Exception e)
            {
                Log.Debug(e);
                Log.Debug("erm ,,.,. failed to export artiact icon with proper name ,,. trying with tokenm !! " + Language.GetString(artifactDef.nameToken));
                exportTexture(artifactDef.smallIconSelectedSprite, Path.Combine(temp, artifactDef.nameToken + " Selected.png"));
                exportTexture(artifactDef.smallIconDeselectedSprite, Path.Combine(temp, artifactDef.nameToken + " Deselected.png"));
            }
        }
    
    }
}