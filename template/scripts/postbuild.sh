#!/bin/bash

buildDir="$1""$3"/bin/"$2"/netstandard2.1/
pcDebugDir="/run/media/icebrah/buh/gale/riskofrain2/profiles/debug 3/BepInEx/plugins/"
weaverDir="/run/media/icebrah/buh/github/code-mods/weaver/"

runBuild () {
  if ! [ -d "$1$3" ]; then
    mkdir "$1$3"
  fi
    
  if [[ "$4" == "Weaver" ]]; then 
      wine "$weaverDir"/Unity.UNetWeaver.exe  "$weaverDir"/libs/UnityEngine.CoreModule.dll "$weaverDir"/libs/com.unity.multiplayer-hlapi.Runtime.dll "$1" "$1""$3.dll" "$weaverDir"/libs/
  fi
    
  cp "$buildDir""$3.dll" "$1$3/""$3.dll"
  cp "$buildDir""$3.pdb" "$1$3/""$3.pdb"
}

if [ -d "$pcDebugDir" ]; then
  runBuild "$pcDebugDir" "$2" "$3" "$4"
fi
