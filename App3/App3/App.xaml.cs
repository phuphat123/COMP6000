using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


[assembly: ExportFont("Clear.ttf")]

namespace App3
{
    public partial class App : Application
    {
        
        public App()
        {
            InitializeComponent();
            
            MainPage = new NavigationPage(new Page1());
            ((NavigationPage)MainPage).BarBackgroundColor = Color.FromHex("#FFE633");

        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
