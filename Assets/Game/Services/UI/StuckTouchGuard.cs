using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Frees buttons that stay pressed after quick taps on a touch screen.
    /// </summary>
    /// <remarks>
    /// Unity's legacy touch input for UI Toolkit (<c>InputManagerProvider</c>) can report a touch's up with another
    /// pointer id than its down: it renumbers fingers whenever no reported touch is down in a frame (a finger held still
    /// is not reported). <c>Clickable</c> ignores an up of another pointer, so the button keeps the pointer captured,
    /// stays <c>:active</c> and ignores every later down. Once every finger is lifted no touch may hold a capture, so
    /// each one still held is cancelled with its own pointer id, which resets the button.
    /// </remarks>
    public sealed class StuckTouchGuard
    {
        /// <summary>
        /// Cancels touch captures left over after every finger was lifted. Call once per frame.
        /// </summary>
        /// <param name="panel">The UI document's panel.</param>
        public void Tick(IPanel panel)
        {
            if (panel == null || Input.touchCount > 0)
            {
                return;
            }

            for (var i = 0; i < PointerId.touchPointerCount; i++)
            {
                var pointerId = PointerId.touchPointerIdBase + i;
                if (panel.GetCapturingElement(pointerId) is VisualElement element)
                {
                    Cancel(element, pointerId);
                }
            }
        }

        private static void Cancel(VisualElement element, int pointerId)
        {
            // GetPooled(Touch) gives the event the pointer id touchPointerIdBase + fingerId.
            var touch = new Touch { fingerId = pointerId - PointerId.touchPointerIdBase, phase = TouchPhase.Canceled };
            using (var cancel = PointerCancelEvent.GetPooled(touch))
            {
                element.SendEvent(cancel);
            }

            // Drops a capture held by an element without a cancel handler; a Clickable resets on the capture-out too.
            if (element.HasPointerCapture(pointerId))
            {
                element.ReleasePointer(pointerId);
            }
        }
    }
}
