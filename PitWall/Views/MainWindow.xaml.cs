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
using PitWall.Services;
using PitWall.ViewModels;

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
            await client.Test();
        }
    }

    
}