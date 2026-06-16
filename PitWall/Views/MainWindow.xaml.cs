using PitWall.Models;
using PitWall.Services;
using PitWall.ViewModels;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace PitWall
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();

            Loaded += async (s, e) => await Testy();
        }

        public async Task Testy()
        {
            HttpClient httpClient = new HttpClient();
            OpenF1APIService apiService = new OpenF1APIService(httpClient);
            OpenF1Client client = new OpenF1Client(apiService);
            SessionCatalogService sessionCatalog = new SessionCatalogService(client);
            SessionDataService sessionData = new SessionDataService(client, sessionCatalog);

            SeasonCalendar calendar = await sessionCatalog.GetCalendarAsync(2024);

            foreach (CalendarMeeting meeting in calendar.Meetings)
            {
                Debug.WriteLine($"{meeting.Meeting.MeetingName} - {meeting.Meeting.MeetingKey}");

                foreach (OpenF1Session session in meeting.Sessions)
                {
                    Debug.WriteLine($"  {session.SessionName} - {session.SessionType} - {session.SessionKey}");
                }
            }

            CalendarMeeting silverstone = await sessionCatalog.GetCalendarMeetingAsync(2024, "BEL");

            Debug.WriteLine(silverstone.Meeting.MeetingName);

            foreach (OpenF1Session session in silverstone.Sessions)
            {
                Debug.WriteLine($"{session.SessionName}: {session.SessionKey}");
            }

            OpenF1Session? race = silverstone.GetMainRaceSession();

            if (race is null)
            {
                throw new InvalidOperationException("Silverstone did not contain a main race session.");
            }

            Debug.WriteLine($"Race session key: {race?.SessionKey}");

            StartingGrid silverStoneStartingGrid = await sessionData.LoadStartingGrid(sessionKey: race!.SessionKey);

            if (silverStoneStartingGrid.Entries.Count == 0)
            {
                Debug.WriteLine($"No starting grid published for {silverStoneStartingGrid.Session.CircuitShortName}, {silverStoneStartingGrid.Session.CountryName}.");
            }
            else
            {
                foreach (var entry in silverStoneStartingGrid.Entries)
                {
                    Debug.WriteLine(entry);
                }
            }

            
        }
    }

    
}