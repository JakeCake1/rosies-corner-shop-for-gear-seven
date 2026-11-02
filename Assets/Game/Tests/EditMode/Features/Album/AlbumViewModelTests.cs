using System.Linq;
using Game.Features.Album;
using Game.Services.Album;
using Game.Services.Content;
using Game.Services.Wallet;
using Game.Tests.EditMode.Features.Gameplay;
using Game.Tests.EditMode.Services.Content;
using Game.Tests.EditMode.Services.Localization;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Album
{
    public sealed class AlbumViewModelTests
    {
        private ItemCatalog _catalog;
        private FakeContentService _content;
        private WalletService _wallet;
        private AlbumService _album;
        private AlbumViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ItemCatalog>();
            _catalog.SetContent(TestContent.Items.ToList(), TestContent.Families.ToList());
            _content = new FakeContentService(_catalog);
            _wallet = new WalletService();
            _album = new AlbumService(_content, _wallet);
        }

        [TearDown]
        public void TearDown()
        {
            _viewModel?.Dispose();
            _album.Dispose();
            _wallet.Dispose();
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void Sections_WhenBuilt_UseLocalizedDepartmentNames()
        {
            _viewModel = new AlbumViewModel(_content, _album, new FakeLocalizationService());

            var titles = _viewModel.Sections.Select(s => s.Title);

            Assert.That(titles, Is.EqualTo(TestContent.Families.Select(f => "family:" + f)));
        }

        [Test]
        public void Entries_WhenItemFound_ShowLocalizedItemName()
        {
            var item = _catalog.Items[0];
            _album.RegisterFound(item.Id);

            _viewModel = new AlbumViewModel(_content, _album, new FakeLocalizationService());

            var entry = EntryOf(item);
            Assert.That(entry.Label, Is.EqualTo("item:" + item.Id));
            Assert.That(entry.IsFound, Is.True);
        }

        [Test]
        public void Entries_WhenItemNotFound_ShowUnknownText()
        {
            _viewModel = new AlbumViewModel(_content, _album, new FakeLocalizationService());

            var entry = EntryOf(_catalog.Items[0]);

            Assert.That(entry.Label, Is.EqualTo(AlbumTexts.UnknownItem));
            Assert.That(entry.IsFound, Is.False);
        }

        [Test]
        public void SectionCount_WhenDepartmentIncomplete_FormatsFoundTotalAndBonus()
        {
            var item = _catalog.Items[0];
            var inFamily = _catalog.Items.Count(i => i.Family == item.Family);
            _album.RegisterFound(item.Id);

            _viewModel = new AlbumViewModel(_content, _album, new FakeLocalizationService());

            var section = _viewModel.Sections.Single(s => s.Title == "family:" + item.Family);
            Assert.That(section.Count, Is.EqualTo($"{AlbumTexts.SectionOpen}(1|{inFamily}|{AlbumService.FamilyBonus})"));
        }

        [Test]
        public void Total_WhenBuilt_FormatsFoundAndCatalogSize()
        {
            _album.RegisterFound(_catalog.Items[0].Id);

            _viewModel = new AlbumViewModel(_content, _album, new FakeLocalizationService());

            Assert.That(_viewModel.Total, Is.EqualTo($"{AlbumTexts.Total}(1|{_catalog.Items.Count})"));
        }

        private AlbumEntry EntryOf(ItemDefinition item) =>
            _viewModel.Sections.Single(s => s.Title == "family:" + item.Family)
                .Entries[_catalog.Items.Where(i => i.Family == item.Family).ToList().IndexOf(item)];
    }
}
