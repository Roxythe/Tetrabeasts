#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ModifierFeedbackAuthoring
{
    public static void Apply()
    {
        const string spritePath = "Assets/Sprites/UI/InversionBoardVFX.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        var assets = GameModeAssets.Load();
        assets.boulderImpactSfx = Clip("BoulderImpact");
        assets.boulderImpactAlternates = new[] { Clip("BoulderStoneThud"), Clip("BoulderStoneCrack"), Clip("BoulderStoneClatter") };
        EditorUtility.SetDirty(assets);
        var ghost = Modifier("Hallucination");
        ghost.hallucinationSpawnSfx = Clip("GhostSpawn");
        ghost.hallucinationMergeSfx = Clip("GhostMerge");
        ghost.hallucinationDeathSfx = Clip("GhostDestroyed");
        EditorUtility.SetDirty(ghost);
        var inversion = Modifier("Inversion");
        inversion.inversionSfx = Clip("InversionActivate");
        inversion.inversionBoardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        inversion.inversionActiveSeconds = 8f; inversion.inversionRestSeconds = 12f;
        inversion.description = "Trust your instincts? Think again! A swirling curse reverses your controls, then fades just long enough to keep you guessing.";
        EditorUtility.SetDirty(inversion);
        var flip = Modifier("FlipTheBoard"); flip.flipSfx = Clip("BoardFlip");
        flip.flipWarningSprite = ImportWarning("FlipBoardWarning"); EditorUtility.SetDirty(flip);
        var bombard = Modifier("Bombardment"); bombard.bombardmentImpactSfx = Clip("BombardmentImpact");
        bombard.bombardmentWarningSprite = ImportWarning("BombardmentWarning"); EditorUtility.SetDirty(bombard);
        var boulder = Modifier("BouldersAway");
        boulder.description = "Heads up! Bouncing boulders batter your beasts and smash through stone. The longer the battle rages, the harder the rocks rain down!";
        EditorUtility.SetDirty(boulder);
        AssetDatabase.SaveAssets();
        Debug.Log("Modifier feedback sprites, audio and descriptions assigned.");
    }
    static Sprite ImportWarning(string name)
    {
        string path = "Assets/Sprites/UI/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static LevelModifierSO Modifier(string name) => AssetDatabase.LoadAssetAtPath<LevelModifierSO>("Assets/ScriptableObjects/LevelModifier/" + name + ".asset");
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelModifier/" + name + ".wav");
}
#endif
