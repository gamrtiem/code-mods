using BNR;
using GoldenCoastPlusRevived.Modules;
using RoR2;
using SS2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BNR.items;
internal class IcetoolDebuff : BuffBase<IcetoolDebuff>
{
    internal override string name => "IcetoolDebuff";
    internal override Sprite icon => Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ElementalRings.texIceRingIcon_png).WaitForCompletion();
    internal override Color color => new Color32(255, 255, 255, 255);
    internal override bool canStack => true;
    internal override bool isDebuff => true;
    internal override EliteDef eliteDef => null;
}
