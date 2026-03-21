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
        bool buildConfirmed;
        bool isRoad;
        Animals selectedAnimal;
        Point CursorCords;
        List<Animals> PlacedAnimals = new List<Animals>();
        int[,] Placements = new int[30, 43];
        DispatcherTimer GameTime = new DispatcherTimer();
        TimeSpan DayTime = new();
        double SunPos;
        public MainWindow()
        {
            InitializeComponent();
            SunPos = Canvas.GetTop(Sun);
            for (int j = 0; j < 43; j++)
            {
                MainGameGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int i = 0; i < 30; i++)
            {
                MainGameGrid.RowDefinitions.Add(new RowDefinition());
            }

            animals = FileManager.ReadFile("animals.txt");

            Grid.SetColumnSpan(MainGameCanvas, 43);
            Grid.SetRowSpan(MainGameCanvas, 30);
            Panel.SetZIndex(MainGameCanvas, 1000);
            RoadButton.Click += Road;
            AnimalsButton.Click += ShowAnimals;
            for (int i = 27; i <= 29; i++)
            {
                for (int j = 17; j <= 25; j++)
                {
                    Placements[i, j] = 2;
                }
            }
            DayTime += TimeSpan.FromHours(6);

            GameTime.Interval = TimeSpan.FromSeconds(0.5);
            GameTime.Tick += GameLoop;
            GameTime.Start();
        }

        //Animals-Images-Placement
        private void ShowAnimals(object sender, RoutedEventArgs e)
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
            PriceTag = new TextBlock() { Text = $"{animal.BuyPrice}$\nPopularity = {animal.Popularity}", FontSize = 36, Background = Brushes.Black, Margin = new Thickness(450,430,0,0), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White};
            MainGameCanvas.Children.Add(PriceTag);
        }
        public void AnimalOnClick(object sender, EventArgs args)
        {
            isRoad = false;
            Button button = sender as Button;
            selectedAnimal = animals.Find(a => a.Type == button.Name);
            for (int i = 0; i < 43; i++)
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
            MainGameGrid.MouseMove += mouseTrackerSnap;
            MainGameCanvas.MouseLeftButtonUp += placeAnimal;
            MainGameCanvas.MouseLeftButtonUp -= placeRoad;
        }
      
        public void mouseTrackerSnap(object sender, MouseEventArgs args)
        {
            MouseTrackingRectangle.Visibility = Visibility.Visible;
            MouseTrackingRectangle.Width = isRoad ? MainGameGrid.ActualWidth / 43 : MainGameGrid.ActualWidth / 43 * 2;
            MouseTrackingRectangle.Height = isRoad ? MainGameGrid.ActualHeight / 30 : MainGameGrid.ActualHeight / 30 * 2;

            Point cursorCords = args.GetPosition(MainGameGrid);

            var pos = CursorPositionConvert(cursorCords);
            bool canPlace;
            if (isRoad)
            {
                canPlace = Placements[pos.row, pos.col] == 0 || Placements[pos.row, pos.col] == 2;
            }
            else
            {
                canPlace =
                    Placements[pos.row, pos.col] == 0 &&
                    Placements[pos.row + 1, pos.col] == 0 &&
                    Placements[pos.row, pos.col + 1] == 0 &&
                    Placements[pos.row + 1, pos.col + 1] == 0;
            }

            MouseTrackingRectangle.Fill = canPlace ? Brushes.LightGray : Brushes.Red;

            Canvas.SetLeft(MouseTrackingRectangle, pos.point.X);
            Canvas.SetTop(MouseTrackingRectangle, pos.point.Y);


        }

        public (Point point, int row, int col) CursorPositionConvert(Point cursorCords)
        {
            double cellWidth = MainGameGrid.ActualWidth / 43;
            double cellHeight = MainGameGrid.ActualHeight / 30;

            int col = (int)(cursorCords.X / cellWidth);
            int row = (int)(cursorCords.Y / cellHeight);

            if (isRoad)
            {
                if (row > 29) row = 29;
                if (col > 42) col = 42;
            }
            else
            {
                if (row > 28) row = 28;
                if (col > 41) col = 41;
            }

            double snappedX = col * cellWidth;
            double snappedY = row * cellHeight;

            return (new Point(snappedX, snappedY), row, col);
        }

        public void placeAnimal(object sender, MouseButtonEventArgs args)
        {
            if (MouseTrackingRectangle.Fill == Brushes.LightGray)
            {
                Border border = new Border() { BorderBrush = Brushes.Black, BorderThickness = new Thickness(2),
                    Width = MouseTrackingRectangle.Width,
                    Height = MouseTrackingRectangle.Height,
                };

                Image image = new Image()
                {
                    Source = new BitmapImage(new Uri($"Images/Animals/{selectedAnimal.Type}.png", UriKind.Relative))
                };
                border.Child = image;
                MainGameCanvas.Children.Add(border);

                var cursorPositions = CursorPositionConvert(args.GetPosition(MainGameGrid));

                Canvas.SetLeft(border, cursorPositions.point.X);
                Canvas.SetTop(border, cursorPositions.point.Y);
                Placements[cursorPositions.row, cursorPositions.col] = 1;

                
                selectedAnimal.cords = cursorPositions.point;
                PlacedAnimals.Add(selectedAnimal);

                for (int i = MainGameGrid.Children.Count - 1; i >= 0; i--)
                {
                    if (MainGameGrid.Children[i] is Border)
                    {
                        MainGameGrid.Children.RemoveAt(i);
                    }
                }
                MouseTrackingRectangle.Visibility = Visibility.Hidden;

                MainGameGrid.MouseMove -= mouseTrackerSnap;
                MainGameCanvas.MouseLeftButtonUp -= placeAnimal;
            }
            
        }

        public void Road(object sender, EventArgs args)
        {
            isRoad = true;
            for (int i = 0; i < 43; i++)
            {
                for (int j = 0; j < 30; j++)
                {
                    Border border = new() { BorderThickness = new Thickness(1), BorderBrush = Brushes.Black };
                    MainGameGrid.Children.Add(border);
                    Grid.SetRow(border, j);
                    Grid.SetColumn(border, i);
                }
            }

            MouseTrackingRectangle.Visibility = Visibility.Visible;

            MainGameGrid.MouseMove -= mouseTrackerSnap;
            MainGameGrid.MouseMove += mouseTrackerSnap;

            MainGameCanvas.MouseLeftButtonUp -= placeAnimal;
            MainGameCanvas.MouseLeftButtonUp -= placeRoad;
            MainGameCanvas.MouseLeftButtonUp += placeRoad;

            Button button = sender as Button;

            button.Click -= Road;
            button.Click += cancelRoadPlacement;

            button.Content = "X";
            button.Background = Brushes.Red;
        }
        public void placeRoad(object sender, MouseButtonEventArgs args)
        {
            if (MouseTrackingRectangle.Fill == Brushes.LightGray)
            {
                Rectangle rectangle = new Rectangle() { Fill = Brushes.DarkGray, Width = MouseTrackingRectangle.Width, Height = MouseTrackingRectangle.Height };

                MainGameCanvas.Children.Add(rectangle);

                var cursorPositions = CursorPositionConvert(args.GetPosition(MainGameGrid));


                Canvas.SetLeft(rectangle, cursorPositions.point.X);
                Canvas.SetTop(rectangle, cursorPositions.point.Y);
                Placements[cursorPositions.row, cursorPositions.col] = 1;
                MessageBox.Show(cursorPositions.row.ToString() + ", " + cursorPositions.col.ToString());
            }

        }
        public void cancelRoadPlacement(object sender, EventArgs args)
        {
            MainGameGrid.MouseMove -= mouseTrackerSnap;
            MainGameCanvas.MouseLeftButtonUp -= placeRoad;

            MouseTrackingRectangle.Visibility = Visibility.Hidden;

            for (int i = MainGameGrid.Children.Count - 1; i >= 0; i--)
            {
                if (MainGameGrid.Children[i] is Border)
                {
                    MainGameGrid.Children.RemoveAt(i);
                }
            }

            Button button = sender as Button;

            button.Click -= cancelRoadPlacement;
            button.Click += Road;

            button.Content = "Road";
            button.Background = Brushes.LightBlue;
        }
        public async void GameLoop (object sender, EventArgs args)
        {
            DayTime += TimeSpan.FromMinutes(1);
            TimeBlock.Text = $"{DayTime.Hours}:{DayTime.Minutes}";
            if (DayTime.Hours >= 24)
                DayTime = TimeSpan.FromHours(0);
           

            TimeBlock.Text = $"{DayTime.Hours}:{DayTime.Minutes}";
            if (DayTime.Hours >= 6 && DayTime.Hours < 18)
            {
                timeIcon.Source = new BitmapImage(new Uri("Images/MenuIcons/sun.png", UriKind.Relative));
            }
            else
            {
                timeIcon.Source = new BitmapImage(new Uri($"Images/MenuIcons/moon.png", UriKind.Relative));
            }
            if (DayTime.Hours >= 6 && DayTime.Hours < 10)
            {
                SunPos += 1100 / (4 * 60);
            }
            else if (DayTime.Hours >= 10 && DayTime.Hours > 18)
            {
                SunPos += 400 / (8 * 60);
            }
            else if (DayTime.Hours >= 18 && DayTime.Hours < 21)
            {
                SunPos += 830 / (3 * 60);
            }
            else
            {
                SunPos += 600 / (9 * 60);
            }
            if (SunPos >= 830)
            {
                SunPos = -2100;
                Canvas.SetTop(Sun, SunPos);
            }
            Canvas.SetTop(Sun, SunPos);
        }
    }
}