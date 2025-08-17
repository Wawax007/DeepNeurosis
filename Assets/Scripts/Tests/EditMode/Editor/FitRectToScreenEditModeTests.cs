using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.EditMode.Editor
{
    public class FitRectToScreenEditModeTests
    {
        [Test]
        public void NoCanvas_SetsSizeToScreen()
        {
            // Arrange
            var go = new GameObject("RectOnly", typeof(RectTransform), typeof(FitRectToScreen));
            var rt = go.GetComponent<RectTransform>();

            // Act (Awake/OnEnable executed on add, but ensure size is updated)
            // Force a canvas update tick to be safe
            Canvas.ForceUpdateCanvases();

            // Assert
            float expectedW = Screen.width;
            float expectedH = Screen.height;
            Assert.AreEqual(expectedW, rt.rect.width, 0.5f, "Width should match screen when no Canvas present");
            Assert.AreEqual(expectedH, rt.rect.height, 0.5f, "Height should match screen when no Canvas present");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void OverlayCanvas_SetsSizeToScreen()
        {
            // Arrange
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FitRectToScreen));
            child.transform.SetParent(canvasGo.transform, worldPositionStays: false);
            var rt = child.GetComponent<RectTransform>();

            // Act
            Canvas.ForceUpdateCanvases();

            // Assert
            float expectedW = Screen.width;
            float expectedH = Screen.height;
            Assert.AreEqual(expectedW, rt.rect.width, 0.5f);
            Assert.AreEqual(expectedH, rt.rect.height, 0.5f);

            Object.DestroyImmediate(child);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void CanvasScaler_AdjustsSizeByScaleFactor()
        {
            // Arrange
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 2f; // Expect dimensions to be halved

            var child = new GameObject("Child", typeof(RectTransform), typeof(FitRectToScreen));
            child.transform.SetParent(canvasGo.transform, worldPositionStays: false);
            var rt = child.GetComponent<RectTransform>();

            // Act
            Canvas.ForceUpdateCanvases();

            // Assert
            float expectedW = Screen.width / 2f;
            float expectedH = Screen.height / 2f;
            Assert.AreEqual(expectedW, rt.rect.width, 0.5f);
            Assert.AreEqual(expectedH, rt.rect.height, 0.5f);

            Object.DestroyImmediate(child);
            Object.DestroyImmediate(canvasGo);
        }
    }
}
