using UnityEngine;

namespace Styletor.Jsons
{
    public class SpriteJson
    {
        // These correspond to Sprite.border
        public float BorderLeft;
        public float BorderBottom;
        public float BorderRight;
        public float BorderTop;

        // These correspond to Sprite.pivot, in 0-1 range
        public float PivotX = 0.5f;
        public float PivotY = 0.5f;

        // Sprite.pixelsPerUnit
        public float PixelsPerUnit = 100;

        public SpriteJson()
        {
        }

        public SpriteJson(Sprite sprite)
        {
            var pivotRel = sprite.pivot / sprite.rect.size;
            var border = sprite.border;

            PixelsPerUnit = sprite.pixelsPerUnit;
            PivotX = pivotRel.x;
            PivotY = pivotRel.y;

            BorderLeft = border.x;
            BorderBottom = border.y;
            BorderRight = border.z;
            BorderTop = border.w;
        }
    }
}