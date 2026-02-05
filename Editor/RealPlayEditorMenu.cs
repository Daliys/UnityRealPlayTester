using UnityEditor;
using RealPlayTester.Core;

namespace RealPlayTester.Editor
{
    public static class RealPlayEditorMenu
    {
        private const string MenuPath = "Tools/RealPlayTester/Disable Automation";

        [MenuItem(MenuPath, priority = 100)]
        public static void ToggleAutomation()
        {
            RealPlayEnvironment.GlobalDisable = !RealPlayEnvironment.GlobalDisable;
            Menu.SetChecked(MenuPath, RealPlayEnvironment.GlobalDisable);
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateToggleAutomation()
        {
            Menu.SetChecked(MenuPath, RealPlayEnvironment.GlobalDisable);
            return true;
        }
    }
}
