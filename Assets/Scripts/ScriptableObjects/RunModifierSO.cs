using UnityEngine;

public abstract class RunModifierSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    public abstract void Apply(GameController gc); // Apply to the CURRENT run only

    // Used to remove mid-run
    public virtual void Remove(GameController gc) { }
}
