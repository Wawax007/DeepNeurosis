using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public class FitRectToScreenPlayModeTests
    {
        [Test]
        public void OverlayCanvas_SetsSizeToScreen_WithForcedCanvasUpdates()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FitRectToScreen));
            child.transform.SetParent(canvasGo.transform, worldPositionStays: false);
            var rt = child.GetComponent<RectTransform>();

            // Force the canvas to update rendering callbacks
            Canvas.ForceUpdateCanvases();
            Canvas.ForceUpdateCanvases();

            float expectedW = Screen.width;
            float expectedH = Screen.height;
            Assert.AreEqual(expectedW, rt.rect.width, 0.5f);
            Assert.AreEqual(expectedH, rt.rect.height, 0.5f);

            Object.DestroyImmediate(child);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void ChangingCanvasScalerScaleFactor_UpdatesSizeAfterForcedUpdate()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FitRectToScreen));
            child.transform.SetParent(canvasGo.transform, worldPositionStays: false);
            var rt = child.GetComponent<RectTransform>();

            // Initial update
            Canvas.ForceUpdateCanvases();

            // Change scale and force update again
            scaler.scaleFactor = 2f;
            Canvas.ForceUpdateCanvases();

            float expectedW = Screen.width / 2f;
            float expectedH = Screen.height / 2f;
            Assert.AreEqual(expectedW, rt.rect.width, 0.5f);
            Assert.AreEqual(expectedH, rt.rect.height, 0.5f);

            Object.DestroyImmediate(child);
            Object.DestroyImmediate(canvasGo);
        }

        [Test]
        public void ScreenSpaceCamera_SetsSizeToScreen_NoScaler()
        {
            var camGo = new GameObject("Camera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();

            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FitRectToScreen));
            child.transform.SetParent(canvasGo.transform, worldPositionStays: false);
            var rt = child.GetComponent<RectTransform>();

            Canvas.ForceUpdateCanvases();

            float expectedW = Screen.width;
            float expectedH = Screen.height;
            Assert.AreEqual(expectedW, rt.rect.width, 0.5f);
            Assert.AreEqual(expectedH, rt.rect.height, 0.5f);

            Object.DestroyImmediate(child);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(camGo);
        }

        [Test]
        public void ScreenSpaceCamera_WithCanvasScaler_AdjustsByScaleFactor()
        {
            var camGo = new GameObject("Camera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1.5f;

            var child = new GameObject("Child", typeof(RectTransform), typeof(FitRectToScreen));
            child.transform.SetParent(canvasGo.transform, worldPositionStays: false);
            var rt = child.GetComponent<RectTransform>();

            Canvas.ForceUpdateCanvases();

            float expectedW = Screen.width / 1.5f;
            float expectedH = Screen.height / 1.5f;
            Assert.AreEqual(expectedW, rt.rect.width, 0.5f);
            Assert.AreEqual(expectedH, rt.rect.height, 0.5f);

            Object.DestroyImmediate(child);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(camGo);
        }
    }
}
