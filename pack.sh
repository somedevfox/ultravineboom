mkdir dist
cd dist
mkdir -p BepInEx/plugins
mkdir -p ULTRAKILL_Data/StreamingAssets/mods/vul.somedevfox.vineboom
cp ../bin/Debug/net48/UltrakillVineBoomMod.dll BepInEx/plugins
cp ../assets/funny.mp3 ULTRAKILL_Data/StreamingAssets/mods/vul.somedevfox.vineboom
zip -r ../vineboom.zip *