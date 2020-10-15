using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using nexus.core.logging;
using nexus.protocols.ble;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace Bruco
{
    public partial class App : Application
    {
        public App(IBluetoothLowEnergyAdapter adapter)
        //public App()
        {
            Device.SetFlags(new string[] { "MediaElement_Experimental" });
            InitializeComponent();

            MainPage = new MainPage(adapter);
            //MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
