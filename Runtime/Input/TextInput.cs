using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealPlayTester.Await;
using RealPlayTester.Core;

namespace RealPlayTester.Input
{
    /// <summary>
    /// Helpers for entering text into focused InputField or TMP_InputField.
    /// </summary>
    public static class TextInput
    {
        private static System.Type _tmpInputFieldType;
        private static System.Type GetCachedTMPInputFieldType() => _tmpInputFieldType ??= System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");

        public static async Task Type(string text, float delayBetweenChars = 0.05f)
        {
            if (!RealPlayEnvironment.IsEnabled || string.IsNullOrEmpty(text)) return;

            RealPlayLog.Info($"[INPUT] Type text: \"{text}\"");

            foreach (char c in text)
            {
                await TypeCharacter(c);
                if (delayBetweenChars > 0f) await Wait.Seconds(delayBetweenChars, unscaled: true);
            }
        }

        public static async Task TypeIntoField(string fieldName, string text, float delayBetweenChars = 0.05f)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var go = GameObject.Find(fieldName);
            if (go == null) { RealPlayLog.Warn("Text.TypeIntoField: field '" + fieldName + "' not found."); return; }

            SelectAndActivateField(go);
            await Type(text, delayBetweenChars);
            SyncFieldValue(go, text);
        }

        private static void SelectAndActivateField(GameObject go)
        {
            var es = RealInputUtility.EnsureEventSystem();
            es.SetSelectedGameObject(go);

            var input = go.GetComponent<InputField>();
            if (input != null) { input.Select(); input.ActivateInputField(); } 
            else
            {
                var tmpType = GetCachedTMPInputFieldType();
                var tmp = tmpType != null ? go.GetComponent(tmpType) : null;
                if (tmp != null) { InvokeIfExists(tmp, "Select"); InvokeIfExists(tmp, "ActivateInputField"); }
            }
        }

        private static void SyncFieldValue(GameObject go, string text)
        {
            var inputField = go.GetComponent<InputField>();
            if (inputField != null && inputField.text != text)
            {
                inputField.text = text;
                inputField.ForceLabelUpdate();
                inputField.onValueChanged?.Invoke(inputField.text);
            }
            else
            {
                var tmpType = GetCachedTMPInputFieldType();
                var tmpField = tmpType != null ? go.GetComponent(tmpType) : null;
                if (tmpField != null && GetText(tmpField) != text)
                {
                    SetText(tmpField, text);
                    InvokeIfExists(tmpField, "ForceLabelUpdate");
                    InvokeIfExists(tmpField, "SendOnValueChangedAndUpdateLabel");
                }
            }
        }

        private static async Task TypeCharacter(char c)
        {
            if (InputSystemShim.IsAvailable)
            {
                if (TryMapCharToKeyCode(c, out KeyCode code, out bool shift))
                {
                    if (shift) await InputSystemShim.QueueKeyState(KeyCode.LeftShift, true);
                    await Press.Key(code, 0.05f);
                    if (shift) await InputSystemShim.QueueKeyState(KeyCode.LeftShift, false);
                    await Task.Yield();
                    return;
                }
            }

            var target = FindBestInputTarget();
            if (target == null) { ApplyFallbackCharacter(c); return; }

            if (await ApplyToInputField(target, c)) return;
            await ApplyToTMPField(target, c);
        }

        private static bool TryMapCharToKeyCode(char c, out KeyCode code, out bool shift)
        {
            shift = false;
            if (char.IsLower(c)) { code = (KeyCode)Enum.Parse(typeof(KeyCode), char.ToUpper(c).ToString()); return true; }
            if (char.IsUpper(c)) { code = (KeyCode)Enum.Parse(typeof(KeyCode), c.ToString()); shift = true; return true; }
            if (char.IsDigit(c)) { code = (KeyCode)Enum.Parse(typeof(KeyCode), "Alpha" + c); return true; }
            
            if (TryMapSpecialChar(c, out code)) return true;
            return TryMapShiftedChar(c, out code, out shift);
        }

        private static bool TryMapSpecialChar(char c, out KeyCode code)
        {
            code = c switch {
                ' ' => KeyCode.Space, '\n' => KeyCode.Return, '\r' => KeyCode.Return,
                (char)8 => KeyCode.Backspace, '.' => KeyCode.Period, ',' => KeyCode.Comma,
                '/' => KeyCode.Slash, '-' => KeyCode.Minus, '=' => KeyCode.Equals,
                ';' => KeyCode.Semicolon, '\'' => KeyCode.Quote, '[' => KeyCode.LeftBracket,
                ']' => KeyCode.RightBracket, '\\' => KeyCode.Backslash, '`' => KeyCode.BackQuote,
                _ => KeyCode.None
            };
            return code != KeyCode.None;
        }

        private static bool TryMapShiftedChar(char c, out KeyCode code, out bool shift)
        {
            shift = true;
            code = c switch {
                '!' => KeyCode.Alpha1, '@' => KeyCode.Alpha2, '#' => KeyCode.Alpha3,
                '$' => KeyCode.Alpha4, '%' => KeyCode.Alpha5, '^' => KeyCode.Alpha6,
                '&' => KeyCode.Alpha7, '*' => KeyCode.Alpha8, '(' => KeyCode.Alpha9,
                ')' => KeyCode.Alpha0, '_' => KeyCode.Minus, '+' => KeyCode.Equals,
                ':' => KeyCode.Semicolon, '"' => KeyCode.Quote, '<' => KeyCode.Comma,
                '>' => KeyCode.Period, '?' => KeyCode.Slash, '{' => KeyCode.LeftBracket,
                '}' => KeyCode.RightBracket, '|' => KeyCode.Backslash, '~' => KeyCode.BackQuote,
                _ => KeyCode.None
            };
            if (code == KeyCode.None) shift = false;
            return code != KeyCode.None;
        }

        private static GameObject FindBestInputTarget()
        {
            var es = RealInputUtility.EnsureEventSystem();
            var target = es.currentSelectedGameObject ?? es.firstSelectedGameObject;
            if (target != null) return target;

            var anyInput = UnityEngine.Object.FindFirstObjectByType<InputField>();
            if (anyInput != null) return anyInput.gameObject;

            var tmpType = GetCachedTMPInputFieldType();
            if (tmpType != null)
            {
                var tmpField = UnityEngine.Object.FindFirstObjectByType(tmpType);
                if (tmpField != null) return ((Component)tmpField).gameObject;
            }
            return null;
        }

        private static async Task<bool> ApplyToInputField(GameObject target, char c)
        {
            var input = target.GetComponent<InputField>();
            if (input == null) return false;

            if (c == (char)8) // Backspace
            {
                if (input.text.Length > 0) input.text = input.text.Substring(0, input.text.Length - 1);
            } 
            else if (input.characterLimit <= 0 || input.text.Length < input.characterLimit) input.text += c;

            input.ForceLabelUpdate();
            input.onValueChanged?.Invoke(input.text);
            ExecuteEvents.Execute<IUpdateSelectedHandler>(target, new BaseEventData(EventSystem.current), ExecuteEvents.updateSelectedHandler);
            await Task.Yield();
            return true;
        }

        private static async Task ApplyToTMPField(GameObject target, char c)
        {
            var tmpType = GetCachedTMPInputFieldType();
            if (tmpType == null) return;

            var tmpField = target.GetComponent(tmpType);
            if (tmpField != null)
            {
                string before = GetText(tmpField);
                int limit = GetTMPCharacterLimit(tmpField);

                if (c == (char)8) { if (before.Length > 0) SetText(tmpField, before.Substring(0, before.Length - 1)); } 
                else if (limit <= 0 || before.Length < limit) SetText(tmpField, before + c);

                InvokeIfExists(tmpField, "ForceLabelUpdate");
                InvokeIfExists(tmpField, "SendOnValueChangedAndUpdateLabel");
                ExecuteEvents.Execute<IUpdateSelectedHandler>(target, new BaseEventData(EventSystem.current), ExecuteEvents.updateSelectedHandler);
                await Task.Yield();
            }
        }

        private static int GetTMPCharacterLimit(object tmpField)
        {
            var prop = tmpField.GetType().GetProperty("characterLimit", BindingFlags.Public | BindingFlags.Instance);
            return (int)(prop?.GetValue(tmpField) ?? 0);
        }

        private static string GetText(object tmpField)
        {
            var prop = tmpField.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(tmpField) as string ?? string.Empty;
        }

        private static void SetText(object tmpField, string value)
        {
            var prop = tmpField.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            prop?.SetValue(tmpField, value);
        }

        private static void ApplyFallbackCharacter(char c)
        {
            var fallbackInput = UnityEngine.Object.FindFirstObjectByType<InputField>();
            if (fallbackInput != null)
            {
                if (c == (char)8) { if (fallbackInput.text.Length > 0) fallbackInput.text = fallbackInput.text.Substring(0, fallbackInput.text.Length - 1); } 
                else fallbackInput.text += c;
                fallbackInput.ForceLabelUpdate();
                fallbackInput.onValueChanged?.Invoke(fallbackInput.text);
                return;
            }

            var tmpTypeFallback = GetCachedTMPInputFieldType();
            if (tmpTypeFallback != null)
            {
                var tmpField = UnityEngine.Object.FindFirstObjectByType(tmpTypeFallback);
                if (tmpField != null)
                {
                    string before = GetText(tmpField);
                    if (c == (char)8) { if (before.Length > 0) SetText(tmpField, before.Substring(0, before.Length - 1)); } 
                    else SetText(tmpField, before + c);
                    InvokeIfExists(tmpField, "ForceLabelUpdate");
                    InvokeIfExists(tmpField, "SendOnValueChangedAndUpdateLabel");
                }
            }
        }

        private static void InvokeIfExists(object instance, string methodName, params object[] args)
        {
            if (instance == null) return;
            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(instance, args);
        }
    }
}
