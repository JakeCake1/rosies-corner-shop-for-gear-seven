using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Audio;
using Game.Services.Content;
using Game.Services.Flow;
using Game.Services.Progress;
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
        private readonly IProgressService _progress;
        private readonly IAudioService _audio;

        /// <summary>
        /// Creates the screen controller.
        /// </summary>
        /// <param name="ui">Windows.</param>
        /// <param name="dialogs">Intro card.</param>
        /// <param name="content">Catalog.</param>
        /// <param name="progress">Found items and flags.</param>
        /// <param name="audio">Fanfare for the intro.</param>
        public AlbumScreen(IUIService ui, IDialogService dialogs, IContentService content, IProgressService progress,
            IAudioService audio)
        {
            _ui = ui;
            _dialogs = dialogs;
            _content = content;
            _progress = progress;
            _audio = audio;
        }

        /// <inheritdoc/>
        public async UniTask OpenAsync(CancellationToken ct)
        {
            _progress.Data.AlbumNew = 0;
            _progress.Data.Flags.Add(Flags.AlbumOpened);
            _progress.NotifyChanged();
            await _ui.OpenAsync<AlbumWindow, AlbumViewModel>(new AlbumViewModel(_content, _progress), ct);
        }

        /// <inheritdoc/>
        public async UniTask IntroAsync(CancellationToken ct)
        {
            _progress.SetFlag(Flags.AlbumIntro);
            var open = false;
            var dialog = new DialogViewModel("New: the Album!", RibbonStyle.Green)
            {
                Text = $"Rosie: \"I kept one of everything you sold. {_progress.Data.Found.Count} items so far! " +
                       $"Fill a whole department and I'll pay you {ProgressService.FamilyBonus} coins.\"",
            };
            dialog.Buttons.Add(new DialogButton("Open the album", ButtonStyle.Green, () => open = true));
            dialog.Buttons.Add(new DialogButton("Later", ButtonStyle.WoodSmall, null));
            _audio.Play(SoundId.Win);
            await _dialogs.ShowAsync(dialog, ct);
            if (open) await OpenAsync(ct);
        }
    }
}
