using System.Linq;
using RoR2;
using RoR2.UI;

namespace questshrine.bases;

public abstract class QuestObjectiveBase : ObjectivePanelController.ObjectiveTracker
{
    public QuestBehaviorBase questBehavior;
    private string playerName;

    public string GetPlayerName()
    {
        if (playerName != null) return playerName ?? "";
        
        playerName = questBehavior?.charMaster.playerCharacterMasterController?.GetDisplayName();
        if (LocalUserManager.GetFirstLocalUser().cachedMasterController.GetDisplayName() == playerName)
        {
            playerName = "";
        }
        else
        {
            playerName = $"({playerName}) ";
        }

        return playerName ?? "";
    }
}