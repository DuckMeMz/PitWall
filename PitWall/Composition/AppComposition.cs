using PitWall.Models;
using PitWall.Services;
using PitWall.ViewModels;
using System.Net.Http;

namespace PitWall.Composition;

public static class AppComposition
{
    private static readonly HttpClient HttpClient = new();

    public static MainViewModel CreateMainViewModel()
    {
        OpenF1APIService apiService = new(HttpClient);
        OpenF1Client openF1Client = new(apiService);

        SessionCatalogService sessionCatalog = new(openF1Client);
        SessionDataService sessionData = new(openF1Client);

        ReplayBufferSettings replayBufferSettings = new();
        TrackMapSettings trackMapSettings = new();
        ReplayBuilder replayBuilder = new();
        ReplayLoader replayLoader = new(sessionData, replayBuilder, replayBufferSettings);
        BufferController replayBufferCoordinator = new(replayLoader, replayBufferSettings);
        TrackMapLoader trackMapLoader = new(sessionData, trackMapSettings);

        TrackMapViewModel trackMapViewModel = new(trackMapLoader);
        SessionFinderViewModel sessionFinderViewModel = new(sessionCatalog);

        return new MainViewModel(
            replayLoader,
            replayBufferCoordinator,
            trackMapViewModel,
            sessionFinderViewModel);
    }
}
