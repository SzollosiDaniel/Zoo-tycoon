using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
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
 
        Point CursorCords;

        Animals selectedAnimal;
        Dictionary<(int, int), Animals> PlacedAnimalsCords = new();
        List<Animals> PlacedAnimals = new List<Animals>();

        List<Road> PlacedRoads = new();
        int[,] Placements = new int[30, 43];
        DispatcherTimer GameTime = new();
        TimeSpan DayTime = new();
        DispatcherTimer gameLoop = new();
        TimeSpan gameLoopTime = new();
        Random rnd = new();
        double SunPos;
        User user = new User();
        Dictionary<string, string> LogInInfos = new();
        bool Sell = false;
        public MainWindow()
        {
            InitializeComponent();
            LogInInfos = FileManager.ReadLogInInfos();

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
            RoadButton.Content = $"Road\n25$";
            Grid.SetColumnSpan(MainGameCanvas, 43);
            Grid.SetRowSpan(MainGameCanvas, 30);
            Panel.SetZIndex(MainGameCanvas, 1000);
            RoadButton.Click += Road;
            AnimalsButton.Click += ShowAnimals;
            for (int i = 27; i <= 29; i++)
            {
                for (int j = 17; j <= 25; j++)
                {
                    Placements[i, j] = 10;
                }
            }
            DayTime += TimeSpan.FromHours(6);

            GameTime.Interval = TimeSpan.FromSeconds(0.5);
            GameTime.Tick += TimeLoop;
            GameTime.Start();

            gameLoop.Interval = TimeSpan.FromSeconds(7);
            gameLoop.Tick += GameLoop;
            gameLoop.Start();

            user.Money = 500;

            accountManagementCanvas.Visibility = Visibility.Visible;
            CreateAccountText.MouseUp += SwitchToCreateAccount;
            LogInText.MouseUp += SwitchToLogIn;
            CreateAccountButton.Click += CreateNewAccount;
            LogInButton.Click += LogInAccount;
            SellButton.Click += SellButtonEvent;
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
            
            cancelRoadPlacement(RoadButton, new RoutedEventArgs());
            isRoad = false;
            Button button = sender as Button;
            selectedAnimal = animals.Find(a => a.Type == button.Name);
            if (0 > user.Money - selectedAnimal.BuyPrice)
            {
                MessageBox.Show("Not enough money");
                return;
            }
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
                canPlace = (Placements[pos.row, pos.col] == 0 ||
                           Placements[pos.row, pos.col] == 2 ||
                           Placements[pos.row, pos.col] == 10 ||
                           Placements[pos.row, pos.col] == 3);

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
                    Name = "Index" + (PlacedAnimals.Count).ToString()
                };

                Image image = new Image()
                {
                    Source = new BitmapImage(new Uri($"Images/Animals/{selectedAnimal.Type}.png", UriKind.Relative))
                };
                border.Child = image;
                border.MouseLeftButtonUp += AnimalLeftClick;

                MainGameCanvas.Children.Add(border);

                var cursorPositions = CursorPositionConvert(args.GetPosition(MainGameGrid));

                Canvas.SetLeft(border, cursorPositions.point.X);
                Canvas.SetTop(border, cursorPositions.point.Y);
                Placements[cursorPositions.row, cursorPositions.col] = 1;
                Placements[cursorPositions.row + 1, cursorPositions.col] = 1;
                Placements[cursorPositions.row, cursorPositions.col + 1] = 1;
                Placements[cursorPositions.row + 1, cursorPositions.col + 1] = 1;

                selectedAnimal.Cords = cursorPositions.point;

                PlacedAnimals.Add(selectedAnimal);

                if (0 <= user.Money - selectedAnimal.BuyPrice)
                    user.Money -= selectedAnimal.BuyPrice;
                else
                    MessageBox.Show("Te csóró geci");
                MoneyText.Text = $"{user.Money}$";
                PlacedAnimalsCords.Add((cursorPositions.row, cursorPositions.col), selectedAnimal);
                PlacedAnimalsCords.Add((cursorPositions.row + 1, cursorPositions.col), selectedAnimal);
                PlacedAnimalsCords.Add((cursorPositions.row, cursorPositions.col + 1), selectedAnimal);
                PlacedAnimalsCords.Add((cursorPositions.row + 1, cursorPositions.col + 1), selectedAnimal);
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

                ActivateBuilds();
            }
            
        }

        public void AnimalLeftClick(object sender, MouseButtonEventArgs args)
        {
            Border border = sender as Border;
            Animals animal = PlacedAnimals[Convert.ToInt32(border.Name.Split('x')[1])];
            if (Sell)
            {
                MainGameCanvas.Children.Remove(border);
                user.Money += animal.SellPrice;
                MoneyText.Text = $"{user.Money}$";
            }
            else
            {
                MessageBox.Show("Nope");
            }
        }

        public void SellButtonEvent(object sender, EventArgs args)
        {
            Sell = true;
        }

        public void Road(object sender, EventArgs args)
        {
            if (0 > user.Money - 25)
            {
                MessageBox.Show("Not enough money");
                return;
            }
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
                

                var pos = CursorPositionConvert(args.GetPosition(MainGameGrid));

                if (Placements[pos.row, pos.col] == 3)
                    return;

                Placements[pos.row, pos.col] = 2;

                if (pos.row == 29 && pos.col >= 17 && pos.col <= 25)
                {
                    Placements[pos.row, pos.col] = 3;
                }

                ActivateBuilds();

                PlacedRoads.Add(new Road(pos.point));

                TextBlock rectangle = new TextBlock() { Background = Brushes.DarkGray, Width = MouseTrackingRectangle.Width, Height = MouseTrackingRectangle.Height, Text = $"{Placements[pos.row, pos.col]}"};

                MainGameCanvas.Children.Add(rectangle);
                Canvas.SetLeft(rectangle, pos.point.X);
                Canvas.SetTop(rectangle, pos.point.Y);

                
            }
        }
        
        
        public void ActivateBuilds()
        {
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int i = 0; i < 30; i++)
                {
                    for (int j = 0; j < 43; j++)
                    {
                        if (Placements[i, j] == 2)
                        {
                            if (IsNear(i, j, 3))
                            {
                                Placements[i, j] = 3;
                                changed = true;
                            }
                        }
                        if (Placements[i, j] == 1 && IsNear(i, j, 3))
                        {
                            ActivateAnimal(i, j);
                            changed = true;
                        }
                    }
                }
                
            }
        }
        public void ActivateAnimal(int i, int j)
        {
            if (!PlacedAnimalsCords.ContainsKey((i, j)))
                return;

            Animals animal = PlacedAnimalsCords[(i, j)];

            foreach (var cell in PlacedAnimalsCords)
            {
                if (cell.Value == animal)
                {
                    Placements[cell.Key.Item1, cell.Key.Item2] = 4;
                }
            }

            animal.Active = true;
        }
        public bool IsNear(int i, int j, int value)
        {
            if (i + 1 < 30 && Placements[i + 1, j] == value) return true;
            if (i - 1 >= 0 && Placements[i - 1, j] == value) return true;
            if (j + 1 < 43 && Placements[i, j + 1] == value) return true;
            if (j - 1 >= 0 && Placements[i, j - 1] == value) return true;

            return false;
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

        public async void TimeLoop(object sender, EventArgs args)
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

        public void GameLoop(object sender, EventArgs args)
        {
            HashSet<Animals> asd = PlacedAnimalsCords.Values.ToHashSet();
            int maxPopularity = asd.Where(a => a.Active).Sum(a => a.Popularity);
            if (user.Costumers < maxPopularity - 5 && rnd.Next(1, 101) > 25 && maxPopularity != 0)
            {
                gameLoop.Interval = TimeSpan.FromSeconds(2);
                user.Costumers += 4;
                user.Money += 16 * 2 + 8 * 2; //felnőtt és gyerek jegy
            }
            else if (user.Costumers > maxPopularity + 5 && rnd.Next(1, 101) > 75 && maxPopularity != 0)
            {
                gameLoop.Interval = TimeSpan.FromSeconds(10);
                user.Costumers += 2;
                user.Money += 16 + 8; 
            }
            else if (user.Costumers > maxPopularity + 5 && rnd.Next(1, 101) > 10 && maxPopularity != 0)
                user.Costumers -= 4;
            else if (user.Costumers < maxPopularity - 5 && rnd.Next(1, 101) > 90 && maxPopularity != 0) 
                user.Costumers -= 2;
            else if (rnd.Next(1, 101) > 50 && maxPopularity != 0) { 
                gameLoop.Interval = TimeSpan.FromSeconds(3);
                user.Costumers += 3;
                user.Money += 16 * 2 + 8; 
            }
            else if(rnd.Next(1, 101) > 50 && maxPopularity != 0)
            {
                gameLoop.Interval = TimeSpan.FromSeconds(3);
                user.Costumers -= 3;
            }
            CostumersText.Text = "Customers: " + user.Costumers.ToString();
            MoneyText.Text = $"{user.Money}$";
        }

        public void SwitchToCreateAccount(object sender, MouseButtonEventArgs args)
        {
            LogInCanvas.Visibility = Visibility.Collapsed;
            LogInUsername.Text = "";
            LogInPassword.Password = "";
            CreateCanvas.Visibility = Visibility.Visible;
        }
        public void SwitchToLogIn(object sender, MouseButtonEventArgs args)
        {
            ShowLogin();
        }
        public void ShowLogin()
        {
            LogInCanvas.Visibility = Visibility.Visible;
            CreateUsername.Text = "";
            CreatePassword.Password = "";
            RepeatPassword.Password = "";
            CreateCanvas.Visibility = Visibility.Collapsed;
        }

        public void CreateNewAccount(object sender, EventArgs args)
        {
            if (LogInInfos.ContainsKey(CreateUsername.Text))
                MessageBox.Show("Username already exists");
            
            else if(CreatePassword.Password != RepeatPassword.Password)
                MessageBox.Show("Passwords doesn't match");
            
            else
            {
                FileManager.CreateAccount(CreateUsername.Text, CreatePassword.Password);
                LogInInfos = FileManager.ReadLogInInfos();
                MessageBox.Show("Account Created");
                ShowLogin();
            }
        }
        public void LogInAccount(object sender, EventArgs args)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            if (LogInInfos.ContainsKey(LogInUsername.Text) &&
                BitConverter.ToString(MD5.HashData(utf8.GetBytes(LogInPassword.Password))) == LogInInfos[LogInUsername.Text])
            {
                LogInCanvas.Visibility = Visibility.Collapsed;
                ZooName.Text = LogInUsername.Text.ToString() + "'s zoo";
                LogInUsername.Text = "";
                LogInPassword.Password = "";
                MessageBox.Show("Successful log in");
                accountManagementCanvas.Visibility = Visibility.Collapsed;
            }
            else
            {
                MessageBox.Show("Wrong username or password");
            }
        }
    }
}