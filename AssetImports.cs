using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CustomMaps
{
    class AssetImports
    {
        public static Sprite LoadSprite(string imagePath)
        {
            byte[] imageData = File.ReadAllBytes(imagePath);

            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, imageData);

            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f) // pivot point (centered)
            );
        }
    }

}
    
