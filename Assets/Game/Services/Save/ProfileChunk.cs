using System;
using Newtonsoft.Json.Linq;
using UniRx;

namespace Game.Services.Save
{
    /// <summary>
    /// Base of a service that owns one profile chunk: keeps the typed data, restores it on load or reset and
    /// reports changes so the chunk is saved.
    /// </summary>
    /// <typeparam name="TData">Plain JSON data of the chunk; every field needs a safe default so older saves
    /// without it still load. Don't use the field name <c>version</c>: the save file reserves it.</typeparam>
    /// <remarks>Change <see cref="Data"/> only through the owner's methods and call <see cref="MarkChanged"/>
    /// after each change.</remarks>
    public abstract class ProfileChunk<TData> : IProfileChunk, IDisposable where TData : class, new()
    {
        /// <summary>
        /// The chunk data; a new player's defaults until the profile is loaded.
        /// </summary>
        protected TData Data { get; private set; } = new TData();
        private readonly Subject<Unit> _changed = new Subject<Unit>();

        /// <inheritdoc/>
        object IProfileChunk.Data => Data;

        /// <inheritdoc/>
        public abstract string Key { get; }

        /// <inheritdoc/>
        public virtual int Version => 1;

        /// <inheritdoc/>
        public virtual bool IsKeptOnReset => false;

        /// <inheritdoc/>
        public Type DataType => typeof(TData);

        /// <inheritdoc/>
        public IObservable<Unit> Changed => _changed;

        /// <inheritdoc/>
        public void Restore(object data)
        {
            Data = data as TData ?? new TData();
            OnRestored();
        }

        /// <inheritdoc/>
        public virtual void Migrate(JObject data, int fromVersion)
        {
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            _changed.OnCompleted();
            _changed.Dispose();
        }

        /// <summary>
        /// Called after <see cref="Data"/> was replaced (load or reset); update mirrored reactive state here.
        /// </summary>
        protected virtual void OnRestored()
        {
        }

        /// <summary>
        /// Reports a change of <see cref="Data"/>; the chunk is saved and profile listeners are notified.
        /// </summary>
        protected void MarkChanged() => _changed.OnNext(Unit.Default);
    }
}
