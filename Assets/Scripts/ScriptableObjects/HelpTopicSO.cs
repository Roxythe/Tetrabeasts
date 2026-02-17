using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "Tetrabeasts/Help/Help Topic")]
public class HelpTopicSO : ScriptableObject
{
    public string category;           // Root Folder File name
    public string title;              // Subfolder File name
    [TextArea(4, 20)]
    public string description;

    public Sprite image;
    public VideoClip videoClip;       // optional
}
