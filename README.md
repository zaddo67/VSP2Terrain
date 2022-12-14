# VSP2Terrain
### Vegetation Studio to Terrain

This script will convert data in Vegetation Studio's persistent storage into Unity Terrain vegetation.

**WARNING:** Backup your project before running this script. It will modify your terrain.

The utility can be found under the menu, Tools/Utility/Vegetation Studio to Terrain

To run this tool, drag the game object with the persistent vegetation storage into the persistent storage field then click convert.

## Additional Features
1. You can adjust the scale during the conversion process by using the Scale Factor field
2. You can replace the prefabs used in vegetation studio with alternate prefabs by placing the PrefabReference component on the original prefab, and link this to the new prefab.

I added these features because I was using very large scaling in VSP.  To migrate to Unity Terrain, I had to create new prefabs using new models with the import scale adjusted.

**IMPORTANT:** THis utility only migrates Trees.  If you add the functionality to migrate details, I am happy to take a pull request, or add a link in this readme to your git repository.

