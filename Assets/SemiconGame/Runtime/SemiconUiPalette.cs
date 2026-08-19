using UnityEngine;
using UnityEngine.UI;

namespace SemiconCity.Game
{
    /// <summary>Shared semantic colors used by every runtime interface.</summary>
    public static class SemiconUiPalette
    {
        public static readonly Color32 Ink = new Color32(12, 43, 71, 255);
        public static readonly Color32 Muted = new Color32(48, 78, 99, 255);
        public static readonly Color32 Blue = new Color32(16, 139, 194, 255);
        public static readonly Color32 Mint = new Color32(18, 150, 103, 255);
        public static readonly Color32 Amber = new Color32(184, 103, 0, 255);
        public static readonly Color32 Danger = new Color32(196, 71, 65, 255);
        public static readonly Color32 SelectedButton = new Color32(16, 139, 194, 255);
        public static readonly Color32 IdleButton = new Color32(225, 238, 243, 255);
        public static readonly Color32 LockedButton = new Color32(247, 231, 198, 255);

        public static void SetButtonSelection(Button button, bool selected, bool locked = false)
        {
            if (button == null)
            {
                return;
            }

            if (button.targetGraphic != null)
            {
                button.targetGraphic.color = selected ? SelectedButton : locked ? LockedButton : IdleButton;
            }

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.color = selected ? Color.white : Ink;
            }
        }
    }
}
