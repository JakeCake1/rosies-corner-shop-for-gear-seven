using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Localization;

namespace Game.Tests.EditMode.Services.Localization
{
    /// <summary>
    /// Localization without tables: returns the key itself (<c>key</c>, <c>key(arg1|arg2)</c>, <c>item:42</c>,
    /// <c>family:home</c>), so tests assert which entry and which arguments code asked for.
    /// </summary>
    internal sealed class FakeLocalizationService : ILocalizationService
    {
        public UniTask InitializeAsync(CancellationToken ct) => UniTask.CompletedTask;

        public string Get(string key) => key;

        public string Format(string key, params object[] args) => $"{key}({string.Join("|", args)})";

        public string GetItemName(int itemId) => "item:" + itemId;

        public string GetFamilyName(string family) => "family:" + family;
    }
}
