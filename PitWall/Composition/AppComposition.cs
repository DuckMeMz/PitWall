using PitWall.Services;
using PitWall.ViewModels;
using System.Net.Http;

namespace PitWall.Composition;

public class AppComposition
{
    private static readonly HttpClient HttpClient = new();

    public MainViewModel CreateMainViewModel()
    {
        OpenF1APIService apiService = new(HttpClient);
        OpenF1Client openF1Client = new(apiService);

        SessionCatalogService sessionCatalog = new(openF1Client);
        SessionDataService sessionData = new(openF1Client);

        ReplayBuilder replayBuilder = new();
        ReplayLoader replayLoader = new(sessionData, replayBuilder);

        SessionFinderViewModel sessionFinderViewModel = new(sessionCatalog);

        return new MainViewModel(replayLoader, sessionFinderViewModel);
    }
}
