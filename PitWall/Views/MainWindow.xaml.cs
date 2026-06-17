using PitWall.Models;
using PitWall.Services;
using PitWall.ViewModels;
using System.ComponentModel.DataAnnotations;
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
        }

        public async Task Testy()
        {
            HttpClient httpClient = new HttpClient();
            OpenF1APIService apiService = new OpenF1APIService(httpClient);
            OpenF1Client client = new OpenF1Client(apiService);
            SessionCatalogService sessionCatalog = new SessionCatalogService(client);
            SessionDataService sessionData = new SessionDataService(client, sessionCatalog);
            ReplayFrameBuilder replayFrameBuilder = new ReplayFrameBuilder();

            CalendarMeeting silverstoneMeeting = await sessionCatalog.GetCalendarMeetingAsync(2025, "silverstone");
            OpenF1Session? silverstoneRace = silverstoneMeeting.GetMainRaceSession();
            if(silverstoneRace is not null)
            {
                ReplayData silverstoneReplayData = await sessionData.LoadReplayDataAsync(
                    silverstoneRace.SessionKey
                );

                IReadOnlyList<ReplayFrame> replayFrames = replayFrameBuilder.BuildFrames(silverstoneReplayData);
            } 
        }
    }

    
}