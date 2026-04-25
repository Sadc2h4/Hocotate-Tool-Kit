# ・ver1.25a
┣ Changed the console/version display from 1.23c to 1.25a; updated related assembly metadata and README links.
┣ Updated console output to English-only text; replaced non-ASCII progress marks with ASCII "OK" output.
┣ Fixed --bmd2fbx: the detected armature/bone hierarchy root node (for example, nodes[0]) is now renamed directly to skeleton_root instead of adding an extra wrapper node.
┣ Verified --bmd2fbx with piki_p2_black.bmd; output FBX now contains Model::skeleton_root and exports GLB alongside FBX.
┣ Verified --fbx2bmd round-trip conversion using the generated FBX; skeleton_root is detected and bone data is preserved when required textures are present.
┣ Rebuilt and repackaged Hocotate_Toolkit.exe, BMD_analysis.exe, DiscExtract.exe, DiscRebuild.exe, and FBX_analysis.exe for the release folder.
┗ Updated the distribution package under publish/win-x64 and regenerated publish/win-x64.zip.

# ・ver1.23c
┣ Rebuilt BMD_analysis.exe (based on SuperBMD 2.4.8.0) with --noskeleton fallback support; renamed display to "BMD_analysis v2".
┣ Replaced FBX_analysis.exe with a new build based on MeltyTool / Assimp 5.x; renamed display to "FBX_analysis v2".
┣ Fixed --bmd2fbx: output changed from binary FBX to ASCII FBX (FBX 7.5.0) for Assimp 3.x compatibility; GLB file is now also exported alongside FBX.
┣ Fixed --fbx2bmd: FBX exported by FBX_analysis v2 now includes a skeleton_root dummy node, preserving bone data on round-trip conversion.
┣ Fixed --bmd2dae / --bmd2obj: resolved Assimp64.dll architecture mismatch (x64 vs x86) that caused a crash on export.
┣ Reorganized BMD_analysis source folder: renamed SuperBMD → BMD_analysis, SuperBMDLib → BMD_analysisLib; removed unnecessary batch files.
┣ Added BMD_analysis/ and FBX_analysis/ as build projects to HocotateToolkit.sln alongside existing DiscExtract / DiscRebuild.
┗ Updated README and console output to reflect tool name changes and added FBX Conversion Notes section.

# ・ver1.23b
┗A reference error that caused the program to stop when extracting ISO files has been fixed.
