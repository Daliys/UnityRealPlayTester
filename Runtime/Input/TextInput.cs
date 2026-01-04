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
    public static class Text
    {
        /// <summary>
        /// Type text into the currently selected input field, character by character.
        /// </summary>
        public static async Task Type(string text, float delayBetweenChars = 0.05f)
        {
            if (!RealPlayEnvironment.IsEnabled || string.IsNullOrEmpty(text))
            {
                return;
            }

            RealPlayLog.Info($"[INPUT] Type text: \"{text}\"");

            foreach (char c in text)
            {
                await TypeCharacter(c);
                if (delayBetweenChars > 0f)
                {
                    await Wait.Seconds(delayBetweenChars);
                }
            }
        }

        public static async Task TypeIntoField(string fieldName, string text, float delayBetweenChars = 0.05f)
        {
            if (!RealPlayEnvironment.IsEnabled) return;

            var go = GameObject.Find(fieldName);
            if (go == null)
            {
                RealPlayLog.Warn($"Text.TypeIntoField: field '{fieldName}' not found.");
                return;
            }

            SelectAndActivateField(go);
            await Type(text, delayBetweenChars);
            SyncFieldValue(go, text);
        }

        private static void SelectAndActivateField(GameObject go)
        {
            var es = RealInputUtility.EnsureEventSystem();
            es.SetSelectedGameObject(go);

            var input = go.GetComponent<InputField>();
            if (input != null)
            {
                input.Select();
                input.ActivateInputField();
            }
            else
            {
                var tmp = go.GetComponent(GetTMPInputFieldType());
                if (tmp != null)
                {
                    InvokeIfExists(tmp, "Select");
                    InvokeIfExists(tmp, "ActivateInputField");
                }
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
                var tmpType = GetTMPInputFieldType();
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
            var target = FindBestInputTarget();
            if (target == null)
            {
                ApplyFallbackCharacter(c);
                return;
            }

            if (await ApplyToInputField(target, c)) return;
            await ApplyToTMPField(target, c);
        }

        private static GameObject FindBestInputTarget()
        {
            var es = RealInputUtility.EnsureEventSystem();
            var target = es.currentSelectedGameObject ?? es.firstSelectedGameObject;
            if (target != null) return target;

            var anyInput = UnityEngine.Object.FindFirstObjectByType<InputField>();
            if (anyInput != null) return anyInput.gameObject;

            var tmpType = GetTMPInputFieldType();
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

            if (input.characterLimit <= 0 || input.text.Length < input.characterLimit)
            {
                input.text += c;
                input.ForceLabelUpdate();
                input.onValueChanged?.Invoke(input.text);
                ExecuteEvents.Execute<IUpdateSelectedHandler>(target, new BaseEventData(EventSystem.current), ExecuteEvents.updateSelectedHandler);
                await Task.Yield();
            }
            return true;
        }

        private static async Task ApplyToTMPField(GameObject target, char c)
        {
            var tmpType = GetTMPInputFieldType();
            if (tmpType == null) return;

            var tmpField = target.GetComponent(tmpType);
            if (tmpField != null)
            {
                string before = GetText(tmpField);
                SetText(tmpField, before + c);
                InvokeIfExists(tmpField, "ForceLabelUpdate");
                InvokeIfExists(tmpField, "SendOnValueChangedAndUpdateLabel");
                ExecuteEvents.Execute<IUpdateSelectedHandler>(((Component)tmpField).gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.updateSelectedHandler);
                await Task.Yield();
            }
        }

        private static System.Type GetTMPInputFieldType()
        {
            return System.Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
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
                fallbackInput.text += c;
                fallbackInput.ForceLabelUpdate();
                fallbackInput.onValueChanged?.Invoke(fallbackInput.text);
                return;
            }

            var tmpTypeFallback = GetTMPInputFieldType();
            if (tmpTypeFallback != null)
            {
                var tmpField = UnityEngine.Object.FindFirstObjectByType(tmpTypeFallback);
                if (tmpField != null)
                {
                    string before = GetText(tmpField);
                    SetText(tmpField, before + c);
                    InvokeIfExists(tmpField, "ForceLabelUpdate");
                    InvokeIfExists(tmpField, "SendOnValueChangedAndUpdateLabel");
                }
            }
        }

        private static void InvokeIfExists(object instance, string methodName, params object[] args)
        {
            if (instance == null)
            {
                return;
            }

            var type = instance.GetType();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(instance, args);
        }
    }
}
