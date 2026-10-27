using System.IO;using UnityEditor;
// New PNG metadata only. Existing calibrated PPU/pivots and supplied .meta stay untouched.
public sealed class SkillSpriteImportDefaults:AssetPostprocessor {
 void OnPreprocessTexture(){if(!assetImporter.importSettingsMissing||!assetPath.EndsWith(".png",System.StringComparison.OrdinalIgnoreCase))return;int i=assetPath.LastIndexOf("/Sprite/",System.StringComparison.Ordinal);if(i<0||!File.Exists(assetPath.Substring(0,i)+"/Data.asset"))return;var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;}
}
