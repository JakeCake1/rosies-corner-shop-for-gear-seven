using Game.Services.UI.Elements;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Creates pooled board views (shelves, baskets) from their UXML templates.
    /// </summary>
    public static class LayoutViews
    {
        /// <summary>
        /// Instantiates <paramref name="layout"/> and attaches a new view to it. Used as a pool factory.
        /// </summary>
        /// <param name="layout">The view's template.</param>
        /// <typeparam name="T">The view type.</typeparam>
        /// <returns>The attached view; its root ignores picking, the view's own buttons still receive taps.</returns>
        public static T Create<T>(VisualTreeAsset layout) where T : ElementView, new()
        {
            var view = new T();
            var element = layout.Instantiate();
            element.pickingMode = PickingMode.Ignore;
            view.Attach(element);
            return view;
        }
    }
}
