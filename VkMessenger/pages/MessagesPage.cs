﻿using ru.MaxKuzmin.VkMessenger.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;

namespace ru.MaxKuzmin.VkMessenger.Pages
{
    public class MessagesPage : CirclePage
    {
        private readonly CircleListView messagesListView = new CircleListView();
        private readonly Dialog dialog;
        private readonly ObservableCollection<Message> messages = new ObservableCollection<Message>();

        public MessagesPage(Dialog dialog)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            this.dialog = dialog;
            Setup();
        }

        private void Update()
        {
            foreach (var item in Message.GetMessages(dialog.PeerId))
            {
                var found = messages.FirstOrDefault(d => d.Id == item.Id);

                if (found == null)
                    messages.Insert(0, item);
            }
        }

        private void Setup()
        {
            SetBinding(CirclePage.RotaryFocusObjectProperty, new Binding() { Source = messagesListView });
            messagesListView.ItemTemplate = new DataTemplate(() =>
            {
                var cell = new EntryCell();
                cell.SetBinding(EntryCell.TextProperty, nameof(Message.Text));
                return cell;
            });
            Content = messagesListView;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Update();
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync();
            return true;
        }
    }
}
