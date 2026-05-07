#!/bin/bash

buildDir=$1butterscotchnroses/bin/Debug/netstandard2.1/
pcDebugDir="/run/media/icebrah/buh/gale/riskofrain2/profiles/debug 3/BepInEx/plugins/icebro-ButterscotchandRosesBaseMod/"
pcMultiplayerDebugDir="/run/media/icebrah/buh/gale/riskofrain2/profiles/debug 3 client/BepInEx/plugins/icebro-ButterscotchandRosesBaseMod/"
laptopDebugDir="/home/icebrah/.local/share/com.kesomannen.gale/riskofrain2/profiles/debug 3/BepInEx/plugins/icebro-ButterscotchandRosesBaseMod/"

cd /run/media/icebrah/buh/github/code-mods/weaver/
/run/media/icebrah/buh/github/code-mods/weaver/bweh.sh $buildDir "butterscotchnroses.dll"

echo "$pcDebugDir""butterscotchnroses.dll"
if [ -d "$pcDebugDir" ]; then
  cp "$buildDir""butterscotchnroses.dll" "$pcDebugDir""butterscotchnroses.dll"
  cp "$buildDir""butterscotchnroses.pdb" "$pcDebugDir""butterscotchnroses.pdb"
fi
if [ -d "$pcMultiplayerDebugDir" ]; then
  cp "$buildDir""butterscotchnroses.dll" "$pcMultiplayerDebugDir""butterscotchnroses.dll"
  cp "$buildDir""butterscotchnroses.pdb" "$pcMultiplayerDebugDir""butterscotchnroses.pdb"
fi

if [ -d "$laptopDebugDir" ]; then
  cp "$buildDir""butterscotchnroses.dll" "$laptopDebugDir""butterscotchnroses.dll"
  cp "$buildDir""butterscotchnroses.pdb" "$laptopDebugDir""butterscotchnroses.pdb"
fi

echo "copieds !!"