using BNR;
using GoldenCoastPlusRevived.Modules;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ExamplePlugin.items;
internal class WoodToolkitBuff : BuffBase<WoodToolkitBuff>
{
    internal override string name => "Bark";
    internal override Sprite icon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
    internal override Color color => BNRUtils.Color255(191, 126, 211);
    internal override bool canStack => false;
    internal override bool isDebuff => false;
    internal override EliteDef eliteDef => null;
}
