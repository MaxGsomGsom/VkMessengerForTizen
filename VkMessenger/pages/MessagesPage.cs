﻿using ru.MaxKuzmin.VkMessenger.Cells;
using ru.MaxKuzmin.VkMessenger.Clients;
using ru.MaxKuzmin.VkMessenger.Events;
using ru.MaxKuzmin.VkMessenger.Extensions;
using ru.MaxKuzmin.VkMessenger.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;

namespace ru.MaxKuzmin.VkMessenger.Pages
{
    public class MessagesPage : CirclePage
    {
        private readonly StackLayout verticalLayout = new StackLayout();
        private readonly Dialog dialog;

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

        public MessagesPage(Dialog dialog)
        {
            this.dialog = dialog;
            Setup();
            dialog.Messages.Update(dialog.Id, 0, null).ContinueWith(AfterInitialUpdate);
        }

        /// <summary>
        /// If update successfull scroll to most recent message, otherwise show error popup
        /// </summary>
        private void AfterInitialUpdate(Task<Exception> t)
        {
            if (t.Result != null)
            {
                new RetryInformationPopup(
                    t.Result.Message,
                    async () => await dialog.Messages.Update(dialog.Id, 0, null).ContinueWith(AfterInitialUpdate))
                    .Show();
            }
            else
            {
                Scroll();
                messagesListView.ItemAppearing += LoadMoreMessages;
            }
        }

        /// <summary>
        /// Scroll to most recent message
        /// </summary>
        private void Scroll()
        {
            var lastMessage = dialog.Messages.LastOrDefault();
            if (lastMessage != null)
            {
                messagesListView.ScrollTo(lastMessage, ScrollToPosition.Center, false);
            }
        }

        /// <summary>
        /// Scroll to most recent message when page appeared
        /// </summary>
        protected override void OnAppearing()
        {
            Scroll();
            base.OnAppearing();
        }

        /// <summary>
        /// Initial setup of page
        /// </summary>
        private void Setup()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            SetBinding(RotaryFocusObjectProperty, new Binding() { Source = messagesListView });
            messagesListView.ItemsSource = dialog.Messages;
            popupEntryView.Completed += OnTextCompleted;

            verticalLayout.Children.Add(messagesListView);
            verticalLayout.Children.Add(popupEntryView);
            Content = verticalLayout;
            LongPollingClient.OnMessageUpdate += OnMessageUpdate;
        }

        private async void LoadMoreMessages(object sender, ItemVisibilityEventArgs e)
        {
            if (dialog.Messages.All(i => i.Id >= (e.Item as Message).Id))
            {
                await dialog.Messages.Update(dialog.Id, (uint)dialog.Messages.Count + 20, null);
            }
        }

        /// <summary>
        /// Update messages collection
        /// </summary>
        private async void OnMessageUpdate(object sender, MessageEventArgs args)
        {
            var items = args.Data.Where(e => e.DialogId == dialog.Id);

            if (items.Any())
            {
                await dialog.Messages.Update(0, 0, items.Select(e => e.MessageId).ToArray());
                Scroll();
            }
        }

        /// <summary>
        /// Send message, mark all as read
        /// </summary>
        private async void OnTextCompleted(object sender, EventArgs args)
        {
            dialog.SetReadWithMessages();

            try
            {
                await DialogsClient.MarkAsRead(dialog.Id);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            var text = popupEntryView.Text;
            if (!string.IsNullOrEmpty(text))
            {
                try
                {
                    await MessagesClient.Send(text, dialog.Id);
                    popupEntryView.Text = string.Empty;
                }
                catch (Exception e)
                {
                    popupEntryView.Text = text;
                    Logger.Error(e);
                    new RetryInformationPopup(e.Message, () => OnTextCompleted(null, null));
                }
            }
        }

        /// <summary>
        /// Go to previous page
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync();
            return base.OnBackButtonPressed();
        }
    }
}
