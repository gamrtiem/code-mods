#!/bin/bash

buildDir="$1"kinatoolkit/bin/"$2"/netstandard2.1/
pcDebugDir="/run/media/icebrah/buh/gale/riskofrain2/profiles/debug 3/BepInEx/plugins/kina-kinaToolkit/"
weaverDir="/run/media/icebrah/buh/github/code-mods/weaver/"

runBuild () {
  if [[ "$2" == *"Weaver"* ]]; then 
      wine "$weaverDir"/Unity.UNetWeaver.exe  "$weaverDir"/libs/UnityEngine.CoreModule.dll "$weaverDir"/libs/com.unity.multiplayer-hlapi.Runtime.dll "$1" "$1""kinatoolkit.dll" "$weaverDir"/libs/
  fi
    
  cp "$buildDir""kinatoolkit.dll" "$1""kinatoolkit.dll"
  cp "$buildDir""kinatoolkit.pdb" "$1""kinatoolkit.pdb"
}

if [ -d "$pcDebugDir" ]; then
  runBuild "$pcDebugDir" "$2"
fi
