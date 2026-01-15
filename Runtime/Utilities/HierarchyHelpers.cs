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
            // Case 1: Standard Unity clones "Object (1)"
            int spaceIdx = name.LastIndexOf(" (");
            if (spaceIdx > 0 && name.EndsWith(")"))
                return name.Substring(0, spaceIdx);

            // Case 2: Sequential suffixes like "Slot_0", "Slot_1"
            // We only strip the LAST underscore if it's followed ONLY by digits
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore > 0 && lastUnderscore < name.Length - 1)
            {
                string suffix = name.Substring(lastUnderscore + 1);
                if (IsDigitsOnly(suffix))
                {
                    // Check if it's a coordinate (multiple underscores)
                    int firstUnderscore = name.IndexOf('_');
                    if (firstUnderscore == lastUnderscore)
                    {
                        return name.Substring(0, lastUnderscore);
                    }
                }
            }

            return name;
        }

        private static bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

        public static bool IsBoring(Transform t)
        {
            // Objects with children are never boring
            if (t.childCount > 0) return false;
            
            // Allow collapsing buttons if they are part of a large repetitive group
            // We'll rely on GetRepetitiveGroupCount to decide.
            
            if (t.GetComponent<UnityEngine.UI.Text>() != null) return false;
            if (t.GetComponent<UnityEngine.UI.Toggle>() != null) return false;
            if (t.GetComponent<UnityEngine.UI.Slider>() != null) return false;
            
            var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            if (tmpType != null && t.GetComponent(tmpType) != null) return false;

            return true;
        }

        private static string GetText(GameObject go)
        {
            var txt = go.GetComponent<UnityEngine.UI.Text>();
            if (txt != null) return txt.text;
            var tmpType = System.Type.GetType("TMPro.TMP_Text, Unity.TextMeshPro");
            var tmp = tmpType != null ? go.GetComponent(tmpType) : null;
            if (tmp != null) return tmpType.GetProperty("text")?.GetValue(tmp) as string;
            return null;
        }
    }
}
