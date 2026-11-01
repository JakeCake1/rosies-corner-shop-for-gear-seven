using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Album;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Onboarding;
using Game.Services.UI;
using Game.Services.UI.Dialogs;

namespace Game.Features.Album
{
    /// <summary>
    /// Default <see cref="IAlbumScreen"/>: the album screen (prototype <c>renderCollection</c>) and its intro
    /// popup (prototype <c>albumIntro</c>).
    /// </summary>
    public sealed class AlbumScreen : IAlbumScreen
    {
        private readonly IUIService _ui;
        private readonly IDialogService _dialogs;
        private readonly IContentService _content;
        private readonly IAlbumService _album;
        private readonly IOnboardingService _onboarding;
        private readonly IAudioService _audio;

        /// <summary>
        /// Creates the screen controller.
        /// </summary>
        /// <param name="ui">Windows.</param>
        /// <param name="dialogs">Intro card.</param>
        /// <param name="content">Catalog.</param>
        /// <param name="album">Found items.</param>
        /// <param name="onboarding">Album intro and "opened" flags.</param>
        /// <param name="audio">Fanfare for the intro.</param>
        public AlbumScreen(IUIService ui, IDialogService dialogs, IContentService content, IAlbumService album,
            IOnboardingService onboarding, IAudioService audio)
        {
            _ui = ui;
            _dialogs = dialogs;
            _content = content;
            _album = album;
            _onboarding = onboarding;
            _audio = audio;
        }

        /// <inheritdoc/>
        public async UniTask OpenAsync(CancellationToken ct)
        {
            _album.MarkSeen();
            _onboarding.SetFlag(Flags.AlbumOpened);
            await _ui.OpenAsync<AlbumWindow, AlbumViewModel>(new AlbumViewModel(_content, _album), ct);
        }

        /// <inheritdoc/>
        public async UniTask IntroAsync(CancellationToken ct)
        {
            _onboarding.SetFlag(Flags.AlbumIntro);
            var open = false;
            var dialog = new DialogViewModel("New: the Album!", RibbonStyle.Green)
            {
                Text = $"Rosie: \"I kept one of everything you sold. {_album.Found.Count} items so far! " +
                       $"Fill a whole department and I'll pay you {AlbumService.FamilyBonus} coins.\"",
            };
            dialog.Buttons.Add(new DialogButton("Open the album", ButtonStyle.Green, () => open = true));
            dialog.Buttons.Add(new DialogButton("Later", ButtonStyle.WoodSmall, null));
            _audio.Play(SoundId.Win);
            await _dialogs.ShowAsync(dialog, ct);
            if (open) await OpenAsync(ct);
        }
    }
}
