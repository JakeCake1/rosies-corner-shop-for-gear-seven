using System;
using UniRx;

namespace Game.Services.UI
{
    /// <summary>
    /// Base class of plain C# view models: owns subscriptions and reactive properties and disposes them with itself.
    /// </summary>
    /// <remarks>The UI service disposes a window's view model when the window closes.</remarks>
    public abstract class ViewModel : IDisposable
    {
        /// <summary>
        /// Subscriptions and reactive properties that live as long as the view model.
        /// </summary>
        protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

        /// <summary>
        /// True when the window was closed because another window of its type opened, not by the player. Whoever
        /// waits for an answer must not treat this as a choice.
        /// </summary>
        public bool IsReplaced { get; internal set; }

        /// <inheritdoc/>
        public virtual void Dispose() => Disposables.Dispose();
    }
}
