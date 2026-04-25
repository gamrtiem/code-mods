#!/bin/bash

buildDir=$1butterscotchnroses/bin/Debug/netstandard2.1/
pcDebugDir="/run/media/icebrah/buh/gale/riskofrain2/profiles/debug 3/BepInEx/plugins/icebro-ButterscotchandRosesBaseMod/"
laptopDebugDir="/home/icebrah/.local/share/com.kesomannen.gale/riskofrain2/profiles/debug 3/BepInEx/plugins/icebro-ButterscotchandRosesBaseMod/"

cp "$buildDir""butterscotchnroses.dll" "$pcDebugDir""butterscotchnroses.dll"
cp "$buildDir""butterscotchnroses.pdb" "$pcDebugDir""butterscotchnroses.pdb"

cp "$buildDir""butterscotchnroses.dll" "$laptopDebugDir""butterscotchnroses.dll"
cp "$buildDir""butterscotchnroses.pdb" "$laptopDebugDir""butterscotchnroses.pdb"

echo "copieds !!"