﻿using ru.MaxKuzmin.VkMessenger.Cells;
using ru.MaxKuzmin.VkMessenger.Clients;
using ru.MaxKuzmin.VkMessenger.Extensions;
using ru.MaxKuzmin.VkMessenger.Localization;
using ru.MaxKuzmin.VkMessenger.Models;
using System;   
using System.Linq;
using System.Threading.Tasks;
using ru.MaxKuzmin.VkMessenger.Helpers;
using ru.MaxKuzmin.VkMessenger.Layouts;
using ru.MaxKuzmin.VkMessenger.Managers;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;

namespace ru.MaxKuzmin.VkMessenger.Pages
{
    public class MessagesPage : BezelInteractionPage, IResettable
    {
        private readonly StackLayout verticalLayout = new StackLayout();
        private readonly int dialogId;
        private readonly MessagesManager messagesManager;
        private readonly DialogsManager dialogsManager;

        private readonly SwipeGestureRecognizer swipeLeftRecognizer = new SwipeGestureRecognizer
        {
            Direction = SwipeDirection.Left
        };
        private readonly SwipeGestureRecognizer swipeRightRecognizer = new SwipeGestureRecognizer
        {
            Direction = SwipeDirection.Right
        };

        private readonly CircleListView messagesListView = new CircleListView
        {
            ItemTemplate = new DataTemplate(typeof(MessageCell)),
            HasUnevenRows = true,
            Rotation = 180,
            BarColor = Color.Transparent,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never
        };

        private readonly PopupEntry popupEntryView = new PopupEntry
        {
            IsVisible = false
        };

        public MessagesPage(int dialogId, MessagesManager messagesManager, DialogsManager dialogsManager, bool isInitRequired)
        {
            this.dialogId = dialogId;
            this.messagesManager = messagesManager;
            this.dialogsManager = dialogsManager;

            NavigationPage.SetHasNavigationBar(this, false);
            SetBinding(RotaryFocusObjectProperty, new Binding { Source = messagesListView });
            messagesListView.ItemsSource = messagesManager.GetMessages(dialogId);
            verticalLayout.Children.Add(messagesListView);
            verticalLayout.Children.Add(popupEntryView);
            Content = verticalLayout;

            swipeLeftRecognizer.Command = new Command(OpenKeyboard);
            swipeRightRecognizer.Command = new Command(OnOpenRecorder);
            messagesListView.GestureRecognizers.Add(swipeLeftRecognizer);
            messagesListView.GestureRecognizers.Add(swipeRightRecognizer);

            messagesListView.ItemTapped += OnItemTapped;
            messagesListView.ItemAppearing += OnLoadMoreMessages;
            popupEntryView.Completed += OnTextCompleted;

            if (isInitRequired)
                Appearing += OnAppearing;
        }

        private async void OnAppearing(object s, EventArgs e)
        {
            await InitFromApi();
        }

        /// <summary>
        /// Called on start. If update unsuccessful show error popup and retry
        /// </summary>
        private async Task InitFromApi()
        {
            Appearing -= OnAppearing;

            var messagesCount = messagesManager.GetMessages(dialogId).Count;
            var refreshingPopup = messagesCount > 1 ? null : new InformationPopup { Text = LocalizedStrings.LoadingMessages };
            refreshingPopup?.Show();

            await NetExceptionCatchHelpers.CatchNetException(
                async () =>
                {
                    await messagesManager.UpdateMessagesFromApi(dialogId);
                    //Trim to batch size to prevent skipping new messages between cached and 20 loaded on init
                    messagesManager.TrimMessages(dialogId);
                },
                InitFromApi,
                LocalizedStrings.MessagesNoInternetError,
                true);

            refreshingPopup?.Dismiss();
        }

        private async void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (popupEntryView.IsPopupOpened)
                return;

            await dialogsManager.SetDialogAndMessagesReadAndPublish(dialogId);

            var message = (Message)e.Item;
            if (message.FullScreenAllowed)
                await Navigation.PushAsync(new MessagePage(message));
        }

        /// <summary>
        /// Load more messages when scroll reached the end of the page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnLoadMoreMessages(object sender, ItemVisibilityEventArgs e)
        {
            var message = (Message)e.Item;
            var messages = this.messagesManager.GetMessages(dialogId);
            if (messages.Count >= Consts.BatchSize && messages.All(i => i.Id >= message.Id))
            {
                await messagesManager.UpdateMessagesFromApi(dialogId, messages.Count);
                messagesListView.ScrollIfExist(message, ScrollToPosition.Center);
            }
        }

        /// <summary>
        /// Send message, mark all as read
        /// </summary>
        private async void OnTextCompleted(object? sender = null, EventArgs? args = null)
        {
            await dialogsManager.SetDialogAndMessagesReadAndPublish(dialogId);

            var text = popupEntryView.Text;
            if (string.IsNullOrEmpty(text))
                return;

            await NetExceptionCatchHelpers.CatchNetException(
                async () =>
                {
                    await MessagesClient.Send(dialogId, text, null);
                    popupEntryView.Text = string.Empty;
                },
                () =>
                {
                 OnTextCompleted();
                 return Task.CompletedTask;
                },
                LocalizedStrings.SendMessageNoInternetError,
                false);
        }

        private async void OpenKeyboard()
        {
            await dialogsManager.SetDialogAndMessagesReadAndPublish(dialogId);
            popupEntryView.IsPopupOpened = true;
        }

        private async void OnOpenRecorder()
        {
            await dialogsManager.SetDialogAndMessagesReadAndPublish(dialogId);
            await Navigation.PushAsync(new RecordVoicePage(dialogId));
        }

        protected override void OnDisappearing()
        {
            AudioLayout.PauseAllPlayers();
            base.OnDisappearing();
        }

        public async Task Reset()
        {
            await InitFromApi();
        }
    }
}
