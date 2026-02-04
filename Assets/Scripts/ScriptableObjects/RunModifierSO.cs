using UnityEngine;

public abstract class RunModifierSO : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    public abstract void Apply(GameController gc); // Apply to the current run only

    public virtual void Remove(GameController gc) { } // Used to remove mid-run
}
