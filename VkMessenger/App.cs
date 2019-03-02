﻿using ru.MaxKuzmin.VkMessenger.Clients;
using ru.MaxKuzmin.VkMessenger.Pages;
using Xamarin.Forms;

namespace ru.MaxKuzmin.VkMessenger
{
    public class App : Application
    {
        protected override void OnStart()
        {
#if DEBUG
            DebugSetting.Set();
#endif

            MainPage = new NavigationMainPage();
            //LongPollingClient.Start();
        }

        protected override void OnSleep()
        {
            //LongPollingClient.Stop();
        }

        protected override void OnResume()
        {
            //LongPollingClient.Start();
        }
    }
}
