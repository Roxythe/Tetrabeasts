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
        inversion.description = "Movement and rotation controls reverse for 8 seconds, followed by 12 seconds of normal controls. The reversal sigil wipes away as the effect expires.";
        EditorUtility.SetDirty(inversion);
        var flip = Modifier("FlipTheBoard"); flip.flipSfx = Clip("BoardFlip"); EditorUtility.SetDirty(flip);
        var bombard = Modifier("Bombardment"); bombard.bombardmentImpactSfx = Clip("BombardmentImpact"); EditorUtility.SetDirty(bombard);
        var boulder = Modifier("BouldersAway");
        boulder.description = "Waves of 1-3 boulders fall every 5-10 seconds, accelerating toward 2-4 seconds over three minutes. Up to four can be active. Hits deal 10-15% of a monster's maximum health plus 5 damage. Boulders expire after 4-7 damaging hits or falling off screen.";
        EditorUtility.SetDirty(boulder);
        AssetDatabase.SaveAssets();
        Debug.Log("Modifier feedback sprites, audio and descriptions assigned.");
    }
    static LevelModifierSO Modifier(string name) => AssetDatabase.LoadAssetAtPath<LevelModifierSO>("Assets/ScriptableObjects/LevelModifier/" + name + ".asset");
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LevelModifier/" + name + ".wav");
}
#endif
