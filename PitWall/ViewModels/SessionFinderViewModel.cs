using PitWall.Common;
using PitWall.Commands;
using PitWall.Models;
using PitWall.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace PitWall.ViewModels;

public class SessionFinderViewModel : BindableBase
{
    //Filters
    private int _selectedYear = DateTime.Now.Year;
    private string _searchText = "";
    private bool _showSprintWeekendsOnly = false;
    private CircuitType _circuitTypeFilter = CircuitType.Unknown;


    private SessionFinderMeeting? _selectedMeeting;
    private SessionFinderSession? _selectedSession;
    private bool _isSessionListOpen;
    private bool _isLoadingYear;
    private string _yearLoadStatusText = "Choose a year to load its calendar.";

    public int SelectedYear 
    {
        get => _selectedYear;

        set
        {
            if (SetProperty(ref _selectedYear, value))
            {
                _ = LoadYearAsync(value);
            }
        }
    }

    public bool ShowSprintWeekendsOnly
    {
        get => _showSprintWeekendsOnly;

        set
        {
            if(SetProperty(ref _showSprintWeekendsOnly, value))
            {
                ApplyFilters();
            }
        }
    }

    public CircuitType CircuitTypeFilter
    {
        get => _circuitTypeFilter;

        set
        {
            if(SetProperty(ref _circuitTypeFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    //Connected by two way binding to the search box
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public IReadOnlyList<SessionFinderMeeting> FilteredMeetings => _filteredMeetings;

    public SessionFinderMeeting? SelectedMeeting
    {
        get => _selectedMeeting;
        set
        {
            if (!SetProperty(ref _selectedMeeting, value)) return;
            SelectedSession = null;
            IsSessionListOpen = value is not null;
        }
    }

    public SessionFinderSession? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetProperty(ref _selectedSession, value) && value is not null)
            {
                SessionSelected?.Invoke(value);
            }
        }
    }

    public bool IsSessionListOpen
    {
        get => _isSessionListOpen;
        private set => SetProperty(ref _isSessionListOpen, value);
    }

    public bool IsLoadingYear
    {
        get => _isLoadingYear;
        private set
        {
            if (SetProperty(ref _isLoadingYear, value))
            {
                OnPropertyChanged(nameof(IsYearSelectionEnabled));
            }
        }
    }

    public bool IsYearSelectionEnabled => !IsLoadingYear;

    public string YearLoadStatusText
    {
        get => _yearLoadStatusText;
        private set => SetProperty(ref _yearLoadStatusText, value);
    }

    public IReadOnlyList<int> AvailableYears { get; } =
        Enumerable.Range(
            start: 2023,
            count: DateTime.Now.Year - 2023 + 1)
        .Reverse()
        .ToArray();

    private readonly SessionCatalogService _sessionCatalogService;

    private readonly Dictionary<int, List<SessionFinderMeeting>> _meetingsByYear = [];

    private  List<SessionFinderMeeting> _filteredMeetings = [];

    public ICommand BackToMeetingsCommand { get; }

    public event Action<SessionFinderSession>? SessionSelected;

    public SessionFinderViewModel(SessionCatalogService sessionCatalogService)
    {
        _sessionCatalogService = sessionCatalogService ?? throw new ArgumentNullException(nameof(sessionCatalogService));
        BackToMeetingsCommand = new RelayCommand(BackToMeetings);
    }

    public async Task InitialiseAsync()
    {
        await LoadYearAsync(SelectedYear);
    }

    private async Task LoadYearAsync(int year)
    {
        if (_meetingsByYear.TryGetValue(year, out List<SessionFinderMeeting>? cachedMeetings))
        {
            ApplyFilters();
            YearLoadStatusText = $"Showing {cachedMeetings.Count} Grand Prix weekends for {year}.";
            return;
        }

        IsLoadingYear = true;
        YearLoadStatusText = $"Loading the {year} calendar...";
        _filteredMeetings = [];
        OnPropertyChanged(nameof(FilteredMeetings));

        try
        {
            SeasonCalendar calendar = await _sessionCatalogService.GetCalendarAsync(year);

            List<SessionFinderMeeting> meetings = calendar.Meetings
                .Select(meeting => new SessionFinderMeeting(meeting))
                .ToList();

            _meetingsByYear[year] = meetings;
            ApplyFilters();
            YearLoadStatusText = $"Loaded {meetings.Count} Grand Prix weekends for {year}.";
        }
        catch (Exception exception)
        {
            YearLoadStatusText = $"Could not load {year}: {exception.Message}";
        }
        finally
        {
            IsLoadingYear = false;
        }
    }

    private void ApplyFilters()
    {
        if (!_meetingsByYear.TryGetValue(SelectedYear, out List<SessionFinderMeeting>? meetings))
        {
            _filteredMeetings = [];
        }
        else
        {
            string[] searchTerms = SearchText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            _filteredMeetings = meetings
                .Where(meeting => !ShowSprintWeekendsOnly || meeting.IsSprintWeekend)
                .Where(meeting => CircuitTypeFilter == CircuitType.Unknown || meeting.CircuitType == CircuitTypeFilter)
                .Where(meeting => searchTerms.All(searchTerm =>
                    meeting.SearchableText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        OnPropertyChanged(nameof(FilteredMeetings));
    }

    private void BackToMeetings()
    {
        SelectedMeeting = null;
        IsSessionListOpen = false;
    }
}
