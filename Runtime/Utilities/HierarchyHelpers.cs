using UnityEngine;
using UnityEngine.UI;

namespace RealPlayTester.Utilities
{
    public static class HierarchyHelpers
    {
        public static int GetRepetitiveGroupCount(Transform parent, int startIndex)
        {
            int count = 1;
            Transform first = parent.GetChild(startIndex);
            string baseName = GetBaseName(first.name);
            bool boring = IsBoring(first);

            for (int i = startIndex + 1; i < parent.childCount; i++)
            {
                Transform next = parent.GetChild(i);
                bool sameBase = GetBaseName(next.name) == baseName;
                bool bothBoring = IsBoring(next) && boring;
                
                if (sameBase && bothBoring)
                {
                    count++;
                }
                else
                {
                    break;
                }
            }
            return count;
        }

        public static string GetBaseName(string name)
        {
            // Remove trailing numbers and coordinates like Tile_0_0 or Object (1)
            int underscoreIdx = name.LastIndexOf('_');
            if (underscoreIdx > 0 && underscoreIdx < name.Length - 1 && char.IsDigit(name[underscoreIdx + 1]))
                return name.Substring(0, underscoreIdx);
            
            int spaceIdx = name.LastIndexOf(' ');
            if (spaceIdx > 0 && name.EndsWith(")"))
                return name.Substring(0, spaceIdx);

            return name;
        }

        public static bool IsBoring(Transform t)
        {
            // Objects with no interesting components and no children are boring
            if (t.childCount > 0) return false;
            if (t.GetComponent<Button>() != null) return false;
            if (t.GetComponent<UnityEngine.UI.Text>() != null) return false;
            
            var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            if (tmpType != null && t.GetComponent(tmpType) != null) return false;
            
            return true;
        }
    }
}
