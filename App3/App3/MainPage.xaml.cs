﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Essentials;
using System.Data;
using Xamarin.Forms.Maps;
using Npgsql;
using SQLite;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using System.Globalization;
using App3.Helpers;
using Microcharts.Forms;
using Xamarin.Forms.Shapes;
using Xamarin.Forms.PlatformConfiguration;
using SkiaSharp;

namespace App3
{
    public partial class MainPage : ContentPage


    {
        string connString = "Host=penguin.kent.ac.uk;Username=pp434;Password=rolibb8;Database=pp434";
        NpgsqlConnection _conn;
        int pk;
        Page1 loginPage;

        //MAP Data
        List<Pin> pin;
        Xamarin.Forms.Picker picker;
        Xamarin.Forms.Picker screenTimePicker;
        List<DateTime> availableDays;

        


        public MainPage(Page1 p)
        {
            InitializeComponent();
            BindingContext = this;


            loginPage = p;
            picker = new Xamarin.Forms.Picker
            {
                Title = "Select a Day",
            };

            screenTimePicker = new Xamarin.Forms.Picker
            {
                Title = "Select",
                Items = { "This Week", "Today" },
                
            };

            //actionlisteners
            GPS_Tapped.Tapped += Button_Clicked;
            Motion_Tapped.Tapped += Button_Clicked;
            ScreenTime_Tapped.Tapped += Button_Clicked;
            //TestButton.Clicked += Button_Clicked;
            picker.SelectedIndexChanged += DatePicked;
            screenTimePicker.SelectedIndexChanged += screenTimePicked;
            

            pin = new List<Pin>();


        }
        private bool _isBusy;
        public new bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                ((BoxView)FindByName("Overlay")).IsVisible = _isBusy;
                ((StackLayout)FindByName("BusyIndicator")).IsVisible = _isBusy;
                ((ActivityIndicator)((StackLayout)FindByName("BusyIndicator")).Children[0]).IsRunning = _isBusy;
            }
        }



        Xamarin.Forms.Maps.Map map;

        private Xamarin.Forms.Maps.Polyline _polyline;
        int time;
        ChartView currentChart;
        private async void screenTimePicked(object sender, EventArgs args)
        {
           

            
        }
        

        private void DatePicked(object sender, EventArgs args)
        {
            Debug.WriteLine("DatePicked called!");
            var pins = map.Pins;
            
            for (int i = pins.Count-1; i >= 0; i--)
            {
                map.Pins.RemoveAt(i);
                
            }
            map.MapElements.Remove(_polyline);
            
            plotLocations(availableDays[picker.SelectedIndex]);
            Position tempPos = map.Pins[0].Position;
            var mapSpan = MapSpan.FromCenterAndRadius(tempPos, Distance.FromMeters(500));
            map.MoveToRegion(mapSpan);



        }
        Data GPSpage;
        
        

        private async void Button_Clicked(object sender, EventArgs e)
        {
            try
            {
                //clear datepicker
                pk = loginPage.getPrimaryKey();

                Debug.WriteLine("Button Clicked!");

                var button = sender;
                Data emptyPage = new Data();



                if (button == GPS)          //GPS Button
                {
                    if (GPSpage == null)
                    {
                        IsBusy = true; //loading screen

                        picker.Items.Clear();
                        availableDays = new List<DateTime>();
                        Debug.WriteLine("GPS Clicked");
                        StackLayout s = new StackLayout();
                        s.Children.Add(new Label { Text = "You've clicked GPS!" });
                        s.Children.Add(picker);
                        map = new Xamarin.Forms.Maps.Map();
                        map.HeightRequest = 100;
                        map.WidthRequest = 200;
                        map.MapType = MapType.Street;
                        map.IsShowingUser = false;
                        _conn = new NpgsqlConnection(connString);
                        using (_conn)   //Gathering all the dates avaliable in the database.
                        {
                            _conn.Open();
                            string query = "SELECT DISTINCT date::date FROM coordinates WHERE id = @id";
                            using (var command = new NpgsqlCommand(query, _conn))
                            {
                                command.Parameters.AddWithValue("@id", pk);
                                var reader = command.ExecuteReader();

                                while (reader.Read())
                                {
                                    availableDays.Add(Convert.ToDateTime(reader["date"]));
                                }
                            }
                        }

                        foreach (DateTime day in availableDays) //Displaying all dates in a Picker object
                        {
                            picker.Items.Add(day.ToString("MM/dd/yyyy"));
                        }

                        s.Children.Add(map);
                        emptyPage.Content = s;
                        await Navigation.PushAsync(emptyPage);
                        GPSpage = emptyPage;
                        emptyPage = null;
                        IsBusy = false;

                    }
                    else
                    {
                        await Navigation.PushAsync(GPSpage);
                        IsBusy = false;
                    }
                }



                else if (button == Motion) //Motion Button
                {
                    IsBusy = true;
                    Debug.WriteLine("Motion Clicked");
                    StackLayout s = new StackLayout();

                    s.Children.Add(new Label { Text = "You have clicked motion" });

                    var motionPage = new Motion2();
                    motionPage.Shaken += (Shaken, args) =>
                    {
                        s.Children.Add(new Label { Text = "Shake detected" });
                        emptyPage.Content = s;
                    };
                    motionPage.ToggleAccelerometer();
                    await Navigation.PushAsync(emptyPage);
                    IsBusy = false;
                }
                else if (button == ScreenTime)
                {

                    Debug.WriteLine("ScreenTime Clicked");
                    IsBusy = true;
                    StackLayout s = new StackLayout();
                    


                    Xamarin.Forms.ScrollView scrollview = new Xamarin.Forms.ScrollView();
                    s.Children.Add(scrollview);


                    IAppUsageTracker appUsageTracker = DependencyService.Get<IAppUsageTracker>();
                    if (appUsageTracker.HasUsageAccessGranted())
                    {

                        // Use a background thread to fetch the app usage data
                        await Task.Run(() =>
                        {
                            Dictionary<string, double> appUsageTime = appUsageTracker.GetAppUsageTime();
                            Device.BeginInvokeOnMainThread(() =>
                            {


                                var appUsageData = appUsageTime.ToDictionary(pair => pair.Key, pair => (float)TimeSpan.FromMilliseconds(pair.Value).TotalSeconds);
                                

                                var chart = new Microcharts.BarChart()
                                {
                                    Entries = appUsageData.Select(pair => new Microcharts.ChartEntry((float)pair.Value) { Label = pair.Key, ValueLabel = $"{pair.Value:F2} hrs", Color = SKColor.Parse("#FF1493") }).ToList(),
                                    BackgroundColor = SKColor.Parse("#FFFFFF"),
                                };

                                ChartView c = new ChartView();
                                chart.LabelTextSize = 20;
                                chart.AnimationDuration = TimeSpan.FromMilliseconds(1000);
                                c.Chart = chart;
                                c.HeightRequest = 1500;
                                c.WidthRequest = 1500;

                                currentChart = c;
                                scrollview.Content = currentChart;
                                scrollview.Orientation = ScrollOrientation.Horizontal;

                                emptyPage.Content = s;
                                

                            });
                        });

                    }
                    else
                    {
                        appUsageTracker.RequestUsageAccess();
                        emptyPage.Content = new Label { Text = "no access" };

                    }
                    await Navigation.PushAsync(emptyPage);
                    IsBusy = false;

                }

            }
            catch(Exception ex) { Debug.WriteLine("Error 5: "+ ex.Message); }
        }

        private void plotLocations(DateTime d)
        {
            try
            {
                string query = "SELECT latitude, longitude, date FROM coordinates WHERE id = @id AND date::date = @day ORDER BY date::time ASC";
                _conn = new NpgsqlConnection(connString);
                var cmd = new NpgsqlCommand(query, _conn);
                cmd.Parameters.AddWithValue("@id", pk);
                cmd.Parameters.AddWithValue("@day", d);

                using (_conn)
                {
                    _conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        var positions = new List<Position>();
                        while (reader.Read())
                        {
                            double latitude = reader.GetDouble(0);
                            double longitude = reader.GetDouble(1);
                            DateTime date = reader.GetDateTime(2);
                            var position = new Position(latitude, longitude);
                            map.Pins.Add(new Pin { Type = PinType.Generic,Position = position, Label = date.Day + " , " + date.TimeOfDay });
                            
                            positions.Add(position);
                        }

                        if (positions.Count > 1)
                        {
                            var polyline = new Xamarin.Forms.Maps.Polyline
                            {
                                StrokeColor = Color.Blue,
                                StrokeWidth = 9,
                                //Geopath = { positions }
                            };

                            foreach (var pos in positions)
                            {
                                polyline.Geopath.Add(pos);
                            }
                            _polyline = polyline;
                            map.MapElements.Add(polyline);
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine("Error 3: " + ex.Message); }
        }
        


        public partial class Motion2 : ContentPage
        {
            SensorSpeed speed = SensorSpeed.Game;

            public event EventHandler Shaken;



            public void ToggleAccelerometer()
            {
                try
                {
                    if (Accelerometer.IsMonitoring)
                        Accelerometer.Stop();
                    else
                        Accelerometer.Start(speed);
                }
                catch (FeatureNotSupportedException fnsEx)
                {
                    Console.WriteLine("Feature not supported" + fnsEx);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error" + ex);
                }

                Shaken?.Invoke(this, EventArgs.Empty);
            }
        }




    }
}
