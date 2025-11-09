# Cities Skylines 2 Build Files - By Klyte45

This repository contains useful targets to automate the building and publishing process of mods for Cities Skylines 2.

## Prerequisites

To fully take advantage from `belzont_public.targets` targets, you need to have this set of properties in your mod csproj file:

```xml
<!--Mod basic data-->
<PropertyGroup>
  <ModId>00000</ModId> <!-- PDXMods ID -->
  <Version>0.0.0.0</Version> <!-- Mod DLL version - must follow the pattern A.B.C.D -->
  <GameVersion>1.1.*</GameVersion> <!-- The game version supported, as described on PDXMods development wiki -->
  <ThumbnailPath>Properties/Thumbnail.png</ThumbnailPath><!-- Location of the thumbnail relative to this project -->
  <DisplayName>Mod Name</DisplayName><!-- Display name of the mod -->
  <ShortDescription>A description</ShortDescription><!-- Short description of the mod -->
  <ForumLink>https://forum.paradoxplaza.com/forum/threads/AAAA-AAAA-aaaa/</ForumLink> <!-- Thread of the mod in the PDX Forums, if applicable (leave empty if not) -->
</PropertyGroup>

<!--Mod multíple value data-->
<ItemGroup>
  <ModTag Include="Code Mod" /><!-- Use semicolon to separate the tags -->
  <Screenshots Include="Screenshots/*.jpg" /><!-- Suggested to have a folder where the images are left -->

  <!-- The dependency block is optional and might be repeated serveral times. Example for EUIS dependency: -->
  <Dependency Include="EUIS">
    <ModId>74151</ModId>
    <DisplayName>Extra UI Screens</DisplayName>
  </Dependency>

</ItemGroup>
```

Also, add to your project two files:
- `longDescription.md` containing the description that will be shown on PDXMods site
- `changelog.md` containing the changelog that will be published attached to this version at the PDXMods

Both files will have the XML special characters parsed to their correspondent XML entities (`&<>"` characters).

The default post processor will be disabled. To enable bursting, you need to add the property `<Bursted>True</Bursted>` to your csproj.

## Recommendations

If you want to use this repository as base, I recommend to create a fork from it, since I may change the target file to fit my needs at any time.