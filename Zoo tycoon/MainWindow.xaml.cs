
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
using System.Windows.Threading;

namespace Zoo_tycoon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Animals> animals;
        TextBlock PriceTag;
        DispatcherTimer timer;
        bool inMainGrid;
        bool buildConfirmed;
        Animals selectedAnimal;
        Point CursorCords;
        public MainWindow()
        {
            InitializeComponent();
            
            for (int j = 0; j < 39; j++)
            {
                MainGameGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int i = 0; i < 30; i++)
            {
                MainGameGrid.RowDefinitions.Add(new RowDefinition());
            }

            MenuBarBorder.MouseEnter += (sender, args) => { OpenMenuBar(); };
            animals = FileManager.ReadFile("animals.txt");

            Grid.SetColumnSpan(MainGameCanvas, 39);
            Grid.SetRowSpan(MainGameCanvas, 30);
            Panel.SetZIndex(MainGameCanvas, 1000);
        }

        public void OpenMenuBar()
        {
            MenuBarBorder.Width = 450;
            Button builds = new Button() { Height = 60, Width = 135, Background = Brushes.DeepSkyBlue, Content = "Builds", FontSize = 20 };
            builds.Click += ShowBuilds;
            MenuBar.Children.Add(builds);
            Grid.SetColumn(builds, 0);
            Button AnimalsStat = new Button() { Height = 60, Width = 135, Background = Brushes.DeepSkyBlue, Content = "Shop", FontSize = 20 };
            MenuBar.Children.Add(AnimalsStat);
            Grid.SetColumn(AnimalsStat, 1);
            Button ZooStat = new Button() { Height = 60, Width = 135, Background = Brushes.DeepSkyBlue, Content = "Stats", FontSize = 20 };
            MenuBar.Children.Add(ZooStat);
            Grid.SetColumn(ZooStat, 2);
        }

        private void ShowBuilds(object sender, RoutedEventArgs e)
        {
            List<DockPanel> Dockpanels = new List<DockPanel>();
            int margin = 10;
            int index = 0;
            for (double i = 0; i < animals.Count; i++)
            {
                if (i % 3 == 0 || i == 0)
                {
                    DockPanel dockPanel = new DockPanel() { Height = 150, Width = 424, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, margin, 0, 0), Name = "Dockpanel" + Math.Floor(i/3)};
                    MenuBar.Children.Add(dockPanel);
                    Grid.SetRow(dockPanel, 2);
                    Grid.SetColumnSpan(dockPanel, 3);
                    margin += 160;
                    Dockpanels.Add(dockPanel);
                }
            }
            foreach (Animals item in animals)
            {
                double temp = index / 3;
                Button button = new() {Width = 140, Height = 60, Background = Brushes.Transparent, BorderThickness = new Thickness(0,0,0,0), Name = $"{item.Type}"};
                Image image = new Image() { Source = new BitmapImage(new Uri($"Images/Animals/{item.Type}.png", UriKind.Relative))};
                button.Content = image;
                button.MouseEnter += ShowAnimalsPrice;
                button.MouseLeave += (sender, args) => { MainGameCanvas.Children.Remove(PriceTag); };
                button.Click += AnimalOnClick;
                Dockpanels.Find(a => a.Name == $"Dockpanel{Math.Floor(temp)}").Children.Add(button);
                index++;

            }
        }

        public void ShowAnimalsPrice(object sender, EventArgs args)
        {
            Button button = sender as Button;
            Animals animal = animals.Find(a => a.Type == button.Name); 
            PriceTag = new TextBlock() { Text = $"{animal.BuyPrice}$\nPopularity = {animal.Popularity}", FontSize = 36, Background = Brushes.Black, Margin = new Thickness(450,350,0,0), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White};
            MainGameCanvas.Children.Add(PriceTag);
        }
        public void AnimalOnClick(object sender, EventArgs args)
        {
            Button button = sender as Button;
            selectedAnimal = animals.Find(a => a.Type == button.Name);
            inMainGrid = false;
            for (int i = 0; i < 39; i++)
            {
                for (int j = 0; j < 30; j++)
                {
                    Border border = new() { BorderThickness = new Thickness(1), BorderBrush = Brushes.Black };
                    MainGameGrid.Children.Add(border);
                    Grid.SetRow(border, j);
                    Grid.SetColumn(border, i);
                }
            }
            MainGameGrid.MouseMove -= mouseTrackerSnap;
            MainGameCanvas.MouseLeftButtonUp -= placeAnimal;

            MainGameGrid.MouseMove += mouseTrackerSnap;
            MainGameCanvas.MouseLeftButtonUp += placeAnimal;
        }
      
        public void mouseTrackerSnap(object sender, MouseEventArgs args)
        {
            CursorCords = args.GetPosition(MainGameGrid);

            MouseTrackingRectangle.Visibility = Visibility.Visible;
            MouseTrackingRectangle.Width = MainGameGrid.ActualWidth / 39 * 2;
            MouseTrackingRectangle.Height = MainGameGrid.ActualHeight / 30 * 2;

            double cellWidth = MainGameGrid.ActualWidth / 39;
            double cellHeight = MainGameGrid.ActualHeight / 30;

            int col = (int)(CursorCords.X / cellWidth);
            int row = (int)(CursorCords.Y / cellHeight);

            double snappedX = col * cellWidth;
            double snappedY = row * cellHeight;

            Canvas.SetLeft(MouseTrackingRectangle, snappedX);
            Canvas.SetTop(MouseTrackingRectangle, snappedY);


        }
        public void placeAnimal(object sender, MouseButtonEventArgs args)
        {
            Point pos = args.GetPosition(MainGameGrid);

            double cellWidth = MainGameGrid.ActualWidth / 39;
            double cellHeight = MainGameGrid.ActualHeight / 30;

            int col = (int)(pos.X / cellWidth);
            int row = (int)(pos.Y / cellHeight);

            double snappedX = col * cellWidth;
            double snappedY = row * cellHeight;

            Image image = new Image() { Width = cellWidth * 2, Height = cellHeight * 2, Source = new BitmapImage(new Uri($"Images/Animals/{selectedAnimal.Type}.png", UriKind.Relative)) };

            MainGameCanvas.Children.Add(image);
            Canvas.SetLeft(image, snappedX);
            Canvas.SetTop(image, snappedY);

            for (int i = MainGameGrid.Children.Count - 1; i >= 0; i--)
            {
                if (MainGameGrid.Children[i] is Border)
                {
                    MainGameGrid.Children.RemoveAt(i);
                }
            }
            MouseTrackingRectangle.Visibility = Visibility.Hidden;
        }
    }
}