﻿using ru.MaxKuzmin.VkMessenger.Cells;
using ru.MaxKuzmin.VkMessenger.Clients;
using ru.MaxKuzmin.VkMessenger.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;

namespace ru.MaxKuzmin.VkMessenger.Pages
{
    public class MessagesPage : CirclePage
    {
        private readonly CircleListView messagesListView = new CircleListView
        {
            HorizontalOptions = LayoutOptions.StartAndExpand,
            ItemTemplate = new DataTemplate(typeof(MessageCell)),
            HasUnevenRows = true
        };

        private readonly PopupEntry popupEntryView = new PopupEntry
        {
            VerticalOptions = LayoutOptions.End,
            MaxLength = Message.MaxLength,
            Placeholder = "Type here...",
            HorizontalTextAlignment = TextAlignment.Center,
            FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label)),
            TextColor = Color.White,
            PlaceholderColor = Color.Gray
        };

        private readonly StackLayout verticalLayout = new StackLayout();
        private readonly ObservableCollection<Message> messages = new ObservableCollection<Message>();

        private readonly int dialogId;

        public MessagesPage(int dialogId)
        {
            NavigationPage.SetHasNavigationBar(this, false);

            this.dialogId = dialogId;
            Update();
            Setup();
        }

        private void Update()
        {
            foreach (var item in MessagesClient.GetMessages(dialogId))
            {
                var found = messages.FirstOrDefault(d => d.Id == item.Id);

                if (found == null)
                    messages.Add(item);
                else if (found.Text != item.Text)
                    found.Text = item.Text;
            }
        }

        private void Setup()
        {
            SetBinding(RotaryFocusObjectProperty, new Binding() { Source = messagesListView });
            messagesListView.ItemsSource = messages;
            popupEntryView.Completed += OnSend;

            verticalLayout.Children.Add(messagesListView);
            verticalLayout.Children.Add(popupEntryView);
            Content = verticalLayout;

            LongPollingClient.OnMessageAdd += (s, e) => { if (e.DialogId == dialogId) Update(); };
            LongPollingClient.OnMessageUpdate += (s, e) => { if (e.DialogId == dialogId) Update(); };
        }

        private void OnSend(object sender, EventArgs e)
        {
            MessagesClient.Send(popupEntryView.Text, dialogId);
            popupEntryView.Text = string.Empty;
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync();
            return true;
        }
    }
}
