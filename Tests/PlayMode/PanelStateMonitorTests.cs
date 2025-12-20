using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using RealPlayTester.UI;

namespace RealPlayTester.Tests.PlayMode
{
    [TestFixture]
    public class PanelStateMonitorTests
    {
        private GameObject testPanel;

        [SetUp]
        public void Setup()
        {
            // Create a test panel GameObject
            testPanel = new GameObject("TestPanel");
        }

        [TearDown]
        public void Teardown()
        {
            if (testPanel != null)
            {
                Object.Destroy(testPanel);
            }
        }

        [UnityTest]
        public IEnumerator CheckPanelState_DetectsActivePanel()
        {
            // Arrange
            testPanel.SetActive(true);
            
            yield return null;

            // Act
            var state = PanelStateMonitor.CheckPanelState(testPanel);

            // Assert
            NUnit.Framework.Assert.IsTrue(state.ActiveInHierarchy, "Panel should be active");
        }

        [UnityTest]
        public IEnumerator CheckPanelState_DetectsInactivePanel()
        {
            // Arrange
            testPanel.SetActive(false);
            
            yield return null;

            // Act
            var state = PanelStateMonitor.CheckPanelState(testPanel);

            // Assert
            NUnit.Framework.Assert.IsFalse(state.ActiveInHierarchy, "Panel should be inactive");
            NUnit.Framework.Assert.IsFalse(state.IsVisible, "Inactive panel should not be visible");
        }

        [UnityTest]
        public IEnumerator CheckPanelState_DetectsCanvasGroupAlpha()
        {
            // Arrange
            var canvasGroup = testPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.5f;
            testPanel.SetActive(true);
            
            yield return null;

            // Act
            var state = PanelStateMonitor.CheckPanelState(testPanel);

            // Assert
            NUnit.Framework.Assert.AreEqual(0.5f, state.Alpha, 0.01f, "Alpha should match CanvasGroup");
            NUnit.Framework.Assert.IsTrue(state.HasCanvasGroup, "Should detect CanvasGroup");
        }

        [UnityTest]
        public IEnumerator CheckPanelState_DetectsNonInteractable()
        {
            // Arrange
            var canvasGroup = testPanel.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            testPanel.SetActive(true);
            
            yield return null;

            // Act
            var state = PanelStateMonitor.CheckPanelState(testPanel);

            // Assert
            NUnit.Framework.Assert.IsFalse(state.Interactable, "Panel should not be interactable");
        }

        [UnityTest]
        public IEnumerator CheckPanelState_ReturnsNullStateForNullPanel()
        {
            yield return null;

            // Act
            var state = PanelStateMonitor.CheckPanelState((GameObject)null);

            // Assert
            NUnit.Framework.Assert.IsFalse(state.IsVisible);
            NUnit.Framework.Assert.IsFalse(state.ActiveInHierarchy);
            NUnit.Framework.Assert.IsFalse(state.Interactable);
        }

        [UnityTest]
        public IEnumerator IsPanelReady_ReturnsTrueForVisibleInteractablePanel()
        {
            // Arrange
            var canvasGroup = testPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            testPanel.SetActive(true);
            
            yield return null;

            // Act
            bool isReady = PanelStateMonitor.IsPanelReady(testPanel);

            // Assert
            NUnit.Framework.Assert.IsTrue(isReady, "Panel should be ready");
        }

        [UnityTest]
        public IEnumerator IsPanelReady_ReturnsFalseForInvisiblePanel()
        {
            // Arrange
            var canvasGroup = testPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            testPanel.SetActive(true);
            
            yield return null;

            // Act
            bool isReady = PanelStateMonitor.IsPanelReady(testPanel);

            // Assert
            NUnit.Framework.Assert.IsFalse(isReady, "Invisible panel should not be ready");
        }
    }
}
