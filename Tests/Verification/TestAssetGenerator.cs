using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RealPlayTester.Tests.Verification
{
    // Run this to setup assets for the test
    public class TestAssetGenerator
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        public static void GenerateAssets()
        {
            string dir = "Assets/VerificationTests/Resources";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Generate 128x128 texture
            string path = Path.Combine(dir, "TestTexture_128x128.png");
            if (!File.Exists(path))
            {
                Texture2D tex = new Texture2D(128, 128);
                // Fill with some color
                for (int y = 0; y < 128; y++)
                    for (int x = 0; x < 128; x++)
                        tex.SetPixel(x, y, new Color((float)x/128, (float)y/128, 0f));
                
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();
            }
        }
#endif
    }
}
