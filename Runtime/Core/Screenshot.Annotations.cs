using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealPlayTester.Core
{
    public static partial class Screenshot
    {
        private static Dictionary<string, string> DrawAnnotations(Texture2D tex, List<GameObject> targets)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            int counter = 1;
            char series = 'A';
            int scale = 3; 

            foreach (var go in targets)
            {
                if (go == null) continue;

                Rect rect = GetScreenRect(go);
                if (rect.width <= 0 || rect.height <= 0) continue;

                string label = $"#{series}{counter}";
                map[label] = go.name;

                DrawBox(tex, rect, Color.yellow);
                DrawText(new TextParams { Tex = tex, X = (int)rect.x, Y = (int)rect.yMax - (8 * scale), Text = label, Color = Color.red, Scale = scale });

                if (++counter > 9) { counter = 1; series++; }
            }
            tex.Apply();
            return map;
        }

        private static void DrawBox(Texture2D tex, Rect rect, Color color)
        {
            int xMin = Mathf.Clamp((int)rect.x, 0, tex.width - 1);
            int yMin = Mathf.Clamp((int)rect.y, 0, tex.height - 1);
            int xMax = Mathf.Clamp((int)rect.xMax, 0, tex.width - 1);
            int yMax = Mathf.Clamp((int)rect.yMax, 0, tex.height - 1);

            for (int x = xMin; x <= xMax; x++) { tex.SetPixel(x, yMin, color); tex.SetPixel(x, yMax, color); }
            for (int y = yMin; y <= yMax; y++) { tex.SetPixel(xMin, y, color); tex.SetPixel(xMax, y, color); }
        }

        private struct TextParams
        {
            public Texture2D Tex;
            public int X;
            public int Y;
            public string Text;
            public Color Color;
            public int Scale;
        }

        private static void DrawText(TextParams p)
        {
            int currentX = p.X;
            foreach (char c in p.Text)
            {
                bool[,] bitmap = GetCharBitmap(c);
                for (int r = 0; r < 5; r++)
                {
                    for (int cCol = 0; cCol < 3; cCol++)
                    {
                        if (bitmap[r, cCol]) FillPixel(p.Tex, currentX + (cCol * p.Scale), p.Y + ((4 - r) * p.Scale), p.Scale, p.Color);
                    }
                }
                currentX += (4 * p.Scale);
            }
        }

        private static void FillPixel(Texture2D tex, int px, int py, int scale, Color color)
        {
            for (int sy = 0; sy < scale; sy++)
            {
                for (int sx = 0; sx < scale; sx++)
                {
                    int x = px + sx;
                    int y = py + sy;
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height) tex.SetPixel(x, y, color);
                }
            }
        }

        private static bool[,] GetCharBitmap(char c)
        {
            switch (char.ToUpperInvariant(c))
            {
                case '#': return new bool[,] { {false,true,false}, {true,true,true}, {false,true,false}, {true,true,true}, {false,true,false} };
                case '0': return new bool[,] { {true,true,true}, {true,false,true}, {true,false,true}, {true,false,true}, {true,true,true} };
                case '1': return new bool[,] { {false,true,false}, {true,true,false}, {false,true,false}, {false,true,false}, {true,true,true} };
                case '2': return new bool[,] { {true,true,true}, {false,false,true}, {true,true,true}, {true,false,false}, {true,true,true} };
                case '3': return new bool[,] { {true,true,true}, {false,false,true}, {true,true,true}, {false,false,true}, {true,true,true} };
                case '4': return new bool[,] { {true,false,true}, {true,false,true}, {true,true,true}, {false,false,true}, {false,false,true} };
                case '5': return new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {false,false,true}, {true,true,true} };
                case '6': return new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {true,false,true}, {true,true,true} };
                case '7': return new bool[,] { {true,true,true}, {false,false,true}, {false,true,false}, {false,true,false}, {false,true,false} };
                case '8': return new bool[,] { {true,true,true}, {true,false,true}, {true,true,true}, {true,false,true}, {true,true,true} };
                case '9': return new bool[,] { {true,true,true}, {true,false,true}, {true,true,true}, {false,false,true}, {true,true,true} };
                case 'A': return new bool[,] { {false,true,false}, {true,false,true}, {true,true,true}, {true,false,true}, {true,false,true} };
                case 'B': return new bool[,] { {true,true,false}, {true,false,true}, {true,true,false}, {true,false,true}, {true,true,false} };
                case 'C': return new bool[,] { {true,true,true}, {true,false,false}, {true,false,false}, {true,false,false}, {true,true,true} };
                case 'D': return new bool[,] { {true,true,false}, {true,false,true}, {true,false,true}, {true,false,true}, {true,true,false} };
                case 'E': return new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {true,false,false}, {true,true,true} };
                case 'F': return new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {true,false,false}, {true,false,false} };
                case 'G': return new bool[,] { {true,true,true}, {true,false,false}, {true,false,true}, {true,false,true}, {true,true,true} };
                case 'H': return new bool[,] { {true,false,true}, {true,false,true}, {true,true,true}, {true,false,true}, {true,false,true} };
                case 'I': return new bool[,] { {true,true,true}, {false,true,false}, {false,true,false}, {false,true,false}, {true,true,true} };
                case 'J': return new bool[,] { {false,false,true}, {false,false,true}, {false,false,true}, {true,false,true}, {true,true,true} };
                case 'K': return new bool[,] { {true,false,true}, {true,false,true}, {true,true,false}, {true,false,true}, {true,false,true} };
                case 'L': return new bool[,] { {true,false,false}, {true,false,false}, {true,false,false}, {true,false,false}, {true,true,true} };
                case 'M': return new bool[,] { {true,false,true}, {true,true,true}, {true,true,true}, {true,false,true}, {true,false,true} };
                case 'N': return new bool[,] { {true,true,true}, {true,false,true}, {true,false,true}, {true,false,true}, {true,false,true} };
                case 'O': return new bool[,] { {true,true,true}, {true,false,true}, {true,false,true}, {true,false,true}, {true,true,true} };
                case 'P': return new bool[,] { {true,true,true}, {true,false,true}, {true,true,true}, {true,false,false}, {true,false,false} };
                case 'Q': return new bool[,] { {true,true,true}, {true,false,true}, {true,false,true}, {true,true,true}, {false,false,true} };
                case 'R': return new bool[,] { {true,true,true}, {true,false,true}, {true,true,false}, {true,false,true}, {true,false,true} };
                case 'S': return new bool[,] { {true,true,true}, {true,false,false}, {true,true,true}, {false,false,true}, {true,true,true} };
                case 'T': return new bool[,] { {true,true,true}, {false,true,false}, {false,true,false}, {false,true,false}, {false,true,false} };
                case 'U': return new bool[,] { {true,false,true}, {true,false,true}, {true,false,true}, {true,false,true}, {true,true,true} };
                case 'V': return new bool[,] { {true,false,true}, {true,false,true}, {true,false,true}, {true,false,true}, {false,true,false} };
                case 'W': return new bool[,] { {true,false,true}, {true,false,true}, {true,true,true}, {true,true,true}, {true,false,true} };
                case 'X': return new bool[,] { {true,false,true}, {true,false,true}, {false,true,false}, {true,false,true}, {true,false,true} };
                case 'Y': return new bool[,] { {true,false,true}, {true,false,true}, {true,true,true}, {false,true,false}, {false,true,false} };
                case 'Z': return new bool[,] { {true,true,true}, {false,false,true}, {false,true,false}, {true,false,false}, {true,true,true} };
                default: return new bool[,] { {true,true,true}, {true,true,true}, {true,true,true}, {true,true,true}, {true,true,true} };
            }
        }
    }
}
