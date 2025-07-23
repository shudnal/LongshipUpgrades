# Longship Upgrades
![](https://staticdelivery.nexusmods.com/mods/3667/images/headers/2885_1727651924.jpg)

Ever wanted your most used through the game ship to be even more useful? And not just useful but also cozy and deadly.

This mod is fully compatible with vanilla Longship. You won't lose it after disabling the mod. Except maybe extra storage but anyway destroying ship will create a crates with that extra stored items.

Every mentioned upgrade is highly customizable.

## Features
* upgrade mast (makes it removable and now you can place lantern and tent and switch sails colors)
* place hanging lantern to lighten the deck
* place tent to provide protection from elements and to get an ability to rest while travelling
* upgrade hull with shields to increase ship's health and provide more protection from arrows
* further upgrade hull to make it resistant to fire and ultimately protects from Ashlands ocean
* place a pair of mini-turrets-ballistas on the ship's bow to gun down your enemies
* place any trophy as a ship's figurehead for more style points, or place boss trophy to get an ability to use their forsaken power without overriding your chosen one
* upgrade ship's storage in 2 steps increasing its width and height by 1. Yet that handy extra storage comes at a cost of deck cluttering.
* switch between different styles of ship's heads, sails, tents and shields. Upload your own custom textures with ease.
* you can always remove tent if it interferes with movement on the deck
* place Wisp Torch on the top of the mast to push away the mist

Upgrade materials spend will use craft from containers feature if it is implemented correctly. 

By default every upgrade demands different crafting station and its levels. But default check range is 100 so you doesn't need to park you ship in your forge.

After ship destruction any spent materials or turrets' ammo as well as ship cargo will be safely floating in the boxes. You won't lose anything under water.

## How to upgrade

Every upgradeable part has its own requirements in materials, station type and station level. All of this is configurable.

If you don't know required station or station level you will have "Unknown upgrade" hover text.

If you don't know some material from requirements list it will appear as "Unknown item" along other known items.

To upgrade a specific part you have to find its spot. All spots are tied to its upgradeable parts.
* mast upgrade point is located where mast is fastened to the deck
* lantern upgrade is on its part of a horizontal mast beam
* tent upgrade is on the other side of the horizontal mast beam
* sail switch point is in the middle of the mast (it is disabled if the mast is removed)
* hull upgrade and shields switch is only accessible from inside the ship
* storage upgrade takes up front half of the storage hatch
* turrets upgrade is on the lower side of the bow
* heads switch is located on the plank below the head
* trophy stand could be easily found on the very bow
* tent switcher you can find by looking at tent directly from below or above

## Trophy stand

If you place boss trophy (hey Moder) you will be able to use its forsaken power instead of your current one. It will not override your current power.

If your power is on cooldown you can't use ship's one.

After usage your power will go on the same cooldown.

Used power will no be shared on nearby players, it's always personal.

So its main goal is to extend variability of powers available on your journey. Most likely it will be Moder power useful on a ship only.

## Custom textures for sail, tent and shields

On the first launch mod will create **...\BepInEx\config\shudnal.LongshipUpgrades** directory with subdirectories **sails**, **shields** and **tents**. There will also be *_original.png* variants of that textures taken from the game to use as a reference for new textures.

Then all you need is to place your custom texture in corresponding folder and it will be loaded automatically on the next game launch.

This files have to be shared between clients manually or via a modpack. If some client won't have all the variants nothing will break but amount of available styles for that client will be limited.

Order of applied styles depends on file order in the directory. The files are ordered by name using simple string order algorithm. 

It means it will look like:
* sail_1.png
* sail_10.png
* sail_2.png
* and so on

to make it ordered as you need you should name files like:
* sail_01.png
* sail_02.png
...
* sail_10.png

### Ready to go textures

The mod comes with some built-in textures variants.

You can get good looking sails from [JK - Lore Friendly Sails](https://www.nexusmods.com/valheim/mods/682).

And more stylish shields from [More Round Shield Paints](https://www.nexusmods.com/valheim/mods/254).

As easy as that you can just copy paste png files into corresponging folders as is. Now you have fancy sails and shields.

But no tent sorry. If anyone will ever share their custom tent textures I will happily include it into the mod.

#### To be absolutely certain you got it right

Copy
* texture_shieldwood_d_style.png
* texture_shieldwood_d_style_1.png
* texture_shieldwood_d_style_2.png
* texture_shieldwood_d_style_3.png

from [More Round Shield Paints](https://www.nexusmods.com/valheim/mods/254) archive to **...\BepInEx\config\shudnal.LongshipUpgrades\shields**

Copy every sail png you like from [JK - Lore Friendly Sails](https://www.nexusmods.com/valheim/mods/682) archive to **...\BepInEx\config\shudnal.LongshipUpgrades\sails**. 

You will need to rename the files you copy because all of them has the same name.

Voila.

## Localization
To add your own localization create a file with the name **Longship Upgrades.LanguageName.yml** or **Longship Upgrades.LanguageName.json** anywhere inside of the Bepinex folder. For example, to add a French translation you could create a **Longship Upgrades.French.yml** file inside of the config folder and add French translations there.

Localization file will be loaded on the next game launch or on the next language change.

You can send me a file with your localization at [GitHub](https://github.com/shudnal/LongshipUpgrades/issues) or [Nexus](https://www.nexusmods.com/valheim/mods/2885?tab=posts) so I can add it to mod's bundle.

[Language list](https://valheim-modding.github.io/Jotunn/data/localization/language-list.html).

English localization example is located in `Longship Upgrades.English.json` file next to plugin dll.

## Installation (manual)
extract LongshipUpgrades.dll into your BepInEx\Plugins\ folder

## Configurating
The best way to handle configs is [Configuration Manager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/).

Or [Official BepInEx Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/).

## Mirrors
[Nexus](https://www.nexusmods.com/valheim/mods/2885)

## Credits
* Ephemeral for allowing to use their green, yellow and black vanillaish variants

## Donation
[Buy Me a Coffee](https://buymeacoffee.com/shudnal)

## Discord
[Join server](https://discord.gg/e3UtQB8GFK)