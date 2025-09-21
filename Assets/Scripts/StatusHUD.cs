using UnityEngine;

public class StatusHUD : MonoBehaviour
{
    public StatusIcon shield, magnet, ghost, freeze;
    public Sprite glyphShield, glyphMagnet, glyphGhost, glyphFreeze;

    public Color colorShield = new Color32(0x34,0xC9,0xEB,255);
    public Color colorMagnet = new Color32(0xFF,0xC5,0x42,255);
    public Color colorGhost  = new Color32(0xB8,0x76,0xFF,255);
    public Color colorFreeze = new Color32(0x76,0xD7,0xFF,255);

    public void HideAll()
    {
        if (shield) shield.HideImmediate();
        if (magnet) magnet.HideImmediate();
        if (ghost)  ghost.HideImmediate();
        if (freeze) freeze.HideImmediate();
    }

    public void Show(PowerupId id, float duration)
    {
        switch(id)
        {
            case PowerupId.Shield: if(shield) shield.Show(glyphShield, colorShield, duration); break;
            case PowerupId.Magnet: if(magnet) magnet.Show(glyphMagnet, colorMagnet, duration); break;
            case PowerupId.Ghost:  if(ghost)  ghost.Show(glyphGhost,  colorGhost,  duration); break;
            case PowerupId.Freeze: if(freeze) freeze.Show(glyphFreeze, colorFreeze, duration); break;
        }
    }
}
