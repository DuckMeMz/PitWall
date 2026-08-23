using PitWall.ViewModels;
using PitWall.Composition;
using System.Windows;
using System.Windows.Input;

namespace PitWall
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = AppComposition.CreateMainViewModel();
            DataContext = _viewModel;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitialiseAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }

        private void OnPlaybackSliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel.Playback.BeginScrubbing();
        }

        private void OnPlaybackSliderMouseUp(object sender, MouseButtonEventArgs e)
        {
            _viewModel.Playback.CommitScrub();
        }

        private void OnPlaybackSliderLostMouseCapture(object sender, MouseEventArgs e)
        {
            _viewModel.Playback.CommitScrub();
        }
    }
}
