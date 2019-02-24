﻿using ru.MaxKuzmin.VkMessenger.Models;
using Xamarin.Forms;

namespace ru.MaxKuzmin.VkMessenger.Pages
{
    public class NavigationMainPage : NavigationPage
    {
        public NavigationMainPage()
        {
            SetHasNavigationBar(this, false);

            if (Authorization.Token != null)
                PushAsync(new DialogsPage());
            else
                PushAsync(new AuthorizationPage());
        }
    }
}
