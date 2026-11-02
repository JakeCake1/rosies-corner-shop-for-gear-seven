using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Services.UI.Elements
{
    /// <summary>
    /// Base of every UI Toolkit view: owns one root element, queries its children once in
    /// <see cref="OnAttached"/> and tracks elements it creates at runtime so they can be removed together.
    /// </summary>
    public abstract class ElementView
    {
        private readonly List<VisualElement> _spawned = new List<VisualElement>();

        /// <summary>Root element of the view.</summary>
        public VisualElement Root { get; private set; }

        /// <summary>
        /// Attaches the view to its (already instantiated) element tree.
        /// </summary>
        /// <param name="root">Root element.</param>
        public void Attach(VisualElement root)
        {
            Root = root;
            OnAttached();
        }

        /// <summary>
        /// Queries and caches child elements; called once from <see cref="Attach"/>.
        /// </summary>
        protected abstract void OnAttached();

        /// <summary>
        /// Finds a named descendant and logs an error when the layout lacks it.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="name">Element name in the UXML.</param>
        /// <returns>The element, or null.</returns>
        protected T Q<T>(string name) where T : VisualElement
        {
            var element = Root.Q<T>(name);
            if (element == null)
            {
                Debug.LogError($"[UI] {GetType().Name}: no {typeof(T).Name} named '{name}'");
            }
            return element;
        }

        /// <summary>
        /// Finds a named descendant element.
        /// </summary>
        /// <param name="name">Element name in the UXML.</param>
        /// <returns>The element, or null.</returns>
        protected VisualElement Q(string name) => Q<VisualElement>(name);

        /// <summary>
        /// Adds a runtime-created element and remembers it for <see cref="ClearSpawned"/>.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="parent">Parent element.</param>
        /// <param name="element">New element.</param>
        /// <returns><paramref name="element"/>.</returns>
        protected T Spawn<T>(VisualElement parent, T element) where T : VisualElement
        {
            parent.Add(element);
            _spawned.Add(element);
            return element;
        }

        /// <summary>
        /// Removes every element added with <see cref="Spawn{T}"/>.
        /// </summary>
        protected void ClearSpawned()
        {
            foreach (var element in _spawned)
            {
                element.Kill();
                element.RemoveFromHierarchy();
            }

            _spawned.Clear();
        }

        /// <summary>
        /// Creates a label with USS classes.
        /// </summary>
        /// <param name="text">Text (rich text allowed).</param>
        /// <param name="classes">USS classes.</param>
        /// <returns>The label.</returns>
        public static Label CreateLabel(string text, params string[] classes)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            foreach (var c in classes)
            {
                label.AddToClassList(c);
            }

            return label;
        }

        /// <summary>
        /// Creates an element with USS classes.
        /// </summary>
        /// <param name="classes">USS classes.</param>
        /// <returns>The element.</returns>
        public static VisualElement CreateElement(params string[] classes)
        {
            var element = new VisualElement();
            foreach (var c in classes)
            {
                element.AddToClassList(c);
            }

            return element;
        }
    }
}
