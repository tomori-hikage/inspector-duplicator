using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SIN.Editor
{
    public class LockInspectorParameter : ScriptableSingleton<LockInspectorParameter>
    {
        public List<EditorWindow> WindowList = new List<EditorWindow>();
    }

    public static class LockInspector
    {
        [MenuItem("Window/LockInspector/Tab %l")]
        private static void ShowInspectorWindow()
        {
            if (Selection.activeObject == null)
            {
                return;
            }

            var lockInspectorParameter = LockInspectorParameter.instance;
            lockInspectorParameter.WindowList.RemoveAll(window => window == null);

            var inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor");
            var inspectorWindow = ScriptableObject.CreateInstance(inspectorType) as EditorWindow;
            inspectorWindow.ShowUtility();

            var isLockedPropertyInfo = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public);
            isLockedPropertyInfo.GetSetMethod().Invoke(inspectorWindow, new object[] { true });

            if (lockInspectorParameter.WindowList.Any())
            {
                var lastIndex = lockInspectorParameter.WindowList.Count - 1;
                var lastWindow = lockInspectorParameter.WindowList[lastIndex];
                var position = lastWindow.position;

                // ウィンドウが画面外に行く場合は階段状に表示する
                float positionX;
                float positionY;
                var mainWindowRect = GetMainWindowRect();

                if (position.x + position.width > mainWindowRect.width - position.width)
                {
                    const float windowOffset = 32f;
                    positionX = position.x - windowOffset;
                    positionY = position.y + windowOffset > mainWindowRect.height ? position.y : position.y + windowOffset;
                }
                else
                {
                    positionX = position.x + position.width;
                    positionY = position.y;
                }

                inspectorWindow.position = new Rect(positionX, positionY, position.width, position.height);
            }
            else
            {
                var mainWindowRect = GetMainWindowRect();
                var position = inspectorWindow.position;
                inspectorWindow.position = new Rect((mainWindowRect.width - position.width) * 0.5f, (mainWindowRect.height - position.height) * 0.5f, position.width, position.height);
            }

            lockInspectorParameter.WindowList.Add(inspectorWindow);
            EditorWindow.focusedWindow.Focus();
        }

        [MenuItem("Window/LockInspector/Close Last Tab %#l")]
        private static void CloseLastWindow()
        {
            var lockInspectorParameter = LockInspectorParameter.instance;
            if (!lockInspectorParameter.WindowList.Any())
            {
                return;
            }

            var lastIndex = lockInspectorParameter.WindowList.Count - 1;
            var lastWindow = lockInspectorParameter.WindowList[lastIndex];
            lastWindow.Close();
            lockInspectorParameter.WindowList.RemoveAt(lastIndex);
        }

        [MenuItem("Window/LockInspector/Close All Window %&l")]
        private static void CloseAllWindows()
        {
            var lockInspectorParameter = LockInspectorParameter.instance;
            foreach (var window in lockInspectorParameter.WindowList)
            {
                if (window != null)
                {
                    window.Close();
                }
            }

            lockInspectorParameter.WindowList.Clear();
        }

        private static Rect GetMainWindowRect()
        {
            var containerType = Type.GetType("UnityEditor.ContainerWindow,UnityEditor");
            var showModeFieldInfo = containerType.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
            var positionPropertyInfo = containerType.GetProperty("position", BindingFlags.Public | BindingFlags.Instance);

            var windows = Resources.FindObjectsOfTypeAll(containerType);
            var position = Rect.zero;

            foreach (var window in windows)
            {
                var showmode = (int)showModeFieldInfo.GetValue(window);

                // 4: MainWindow
                if (showmode == 4)
                {
                    position = (Rect)positionPropertyInfo.GetValue(window, null);
                }
            }

            return position;
        }
    }
}