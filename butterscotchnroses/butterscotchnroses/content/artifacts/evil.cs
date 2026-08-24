using BepInEx.Configuration;
using On.RoR2.UI;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace butterscotchnroses.artifacts;

class evil : ArtifactBase<evil>
{
    public static ConfigEntry<int> TimesToPrintMessageOnStart;

    public override string ArtifactName => "Artifact of Example";

    public override string ArtifactLangTokenName => "ARTIFACT_OF_EXAMPLE";

    public override string ArtifactDescription => "When enabled, print a message to the chat at the start of the run.";

    public override Sprite ArtifactEnabledIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion(); // ... disabled

    public override Sprite ArtifactDisabledIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion(); // ..? enabled !!

    public override void Init(ConfigFile config)
    {
        if(true) return;
        CreateConfig(config);
        CreateLang();
        CreateArtifact();
        Hooks();
    }

    private void CreateConfig(ConfigFile config)
    {
        TimesToPrintMessageOnStart = config.Bind<int>("Artifact: " + ArtifactName, "Times to Print Message in Chat", 5, "How many times should a message be printed to the chat on run start?");
    }

    public override void Hooks()
    {
        //Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
        Run.onRunStartGlobal += PrintMessageToChat;
        On.RoR2.UI.GenericNotification.SetItem += GenericNotificationOnSetItem; 
        On.RoR2.UI.ItemIcon.SetItemIndex_ItemIndex_int_float += ItemIconOnSetItemIndex_ItemIndex_int_float;
    }

    private void ItemIconOnSetItemIndex_ItemIndex_int_float(ItemIcon.orig_SetItemIndex_ItemIndex_int_float orig, RoR2.UI.ItemIcon self, ItemIndex newitemindex, int newitemcount, float newdurationpercent)
    {
        orig(self, newitemindex, newitemcount, newdurationpercent);

        if (!ArtifactEnabled) return;
        self.image.texture = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion().texture;
        self.tooltipProvider.titleToken = "???";
        self.tooltipProvider.bodyToken = "???";
        self.tooltipProvider.titleColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Tier1Item);
    }

    private void GenericNotificationOnSetItem(GenericNotification.orig_SetItem orig, RoR2.UI.GenericNotification self, ItemDef itemdef)
    {
        orig(self, itemdef);
        
        if (!ArtifactEnabled) return;
        self.iconImage.texture = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion().texture;
        self.titleText.token = "???";
        self.descriptionText.token = "???";
    }

    private void PrintMessageToChat(Run run)
    {
        if(NetworkServer.active && ArtifactEnabled)
        {
            for(int i = 0; i < TimesToPrintMessageOnStart.Value; i++)
            {
                Chat.AddMessage("Example Artifact has been Enabled.");
            }
        }
    }
}