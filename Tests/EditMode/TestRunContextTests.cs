using NUnit.Framework;
using RealPlayTester.Diagnostics;
using UnityEngine;

namespace RealPlayTester.Tests.EditMode
{
    [TestFixture]
    public class TestRunContextTests
    {
        [Test]
        public void ToJson_IncludesExpectedKeys()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "Test1",
                SceneName = "TestScene",
                LastAction = "ClickButton",
                LastPanel = "MainMenu"
            };
            context.EndTime = context.StartTime.AddSeconds(5);

            // Act
            string json = context.ToJson();

            // Assert
            NUnit.Framework.Assert.IsNotNull(json);
            StringAssert.Contains("\"testName\":", json);
            StringAssert.Contains("\"testId\":", json);
            StringAssert.Contains("\"startTime\":", json);
            StringAssert.Contains("\"endTime\":", json);
            StringAssert.Contains("\"sceneName\":", json);
            StringAssert.Contains("\"unityVersion\":", json);
            StringAssert.Contains("\"packageVersion\":", json);
            StringAssert.Contains("\"activeInputMode\":", json);
            StringAssert.Contains("\"lastAction\":", json);
            StringAssert.Contains("\"lastPanel\":", json);
            StringAssert.Contains("\"lastPlacementAttempt\":", json);
        }

        [Test]
        public void ToJson_HandlesNullPlacementAttempt()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "Test2",
                LastPlacementAttempt = null
            };

            // Act
            string json = context.ToJson();

            // Assert
            StringAssert.Contains("\"lastPlacementAttempt\": null", json);
        }

        [Test]
        public void ToJson_IncludesPlacementAttemptWhenSet()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "Test3",
                LastPlacementAttempt = new PlacementAttempt(
                    new Vector2Int(5, 10),
                    "Factory",
                    "Success"
                )
            };

            // Act
            string json = context.ToJson();

            // Assert
            StringAssert.Contains("\"position\":", json);
            StringAssert.Contains("\"definitionId\":", json);
            StringAssert.Contains("\"result\":", json);
        }

        [Test]
        public void ToMarkdown_GeneratesValidMarkdown()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "Test4",
                SceneName = "GameScene",
                LastAction = "PlaceBuilding"
            };
            context.EndTime = context.StartTime.AddSeconds(2.5);

            // Act
            string markdown = context.ToMarkdown();

            // Assert
            NUnit.Framework.Assert.IsNotNull(markdown);
            StringAssert.Contains("# Test Run Context", markdown);
            StringAssert.Contains("## Test Information", markdown);
            StringAssert.Contains("## Environment", markdown);
            StringAssert.Contains("## Test State", markdown);
            StringAssert.Contains("Test4", markdown);
            StringAssert.Contains("GameScene", markdown);
            StringAssert.Contains("PlaceBuilding", markdown);
        }

        [Test]
        public void ToJson_EscapesSpecialCharacters()
        {
            // Arrange
            var context = new TestRunContext
            {
                TestName = "Test with \"quotes\" and \\backslashes",
                LastAction = "Line1\nLine2"
            };

            // Act
            string json = context.ToJson();

            // Assert
            StringAssert.Contains("\\\"", json); // Escaped quotes
            StringAssert.Contains("\\n", json);  // Escaped newline
            NUnit.Framework.Assert.IsFalse(json.Contains("\"quotes\"")); // Not unescaped
        }
    }
}
