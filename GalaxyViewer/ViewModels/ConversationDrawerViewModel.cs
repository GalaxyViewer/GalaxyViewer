using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalaxyViewer.Models;
using System.Windows.Input;

namespace GalaxyViewer.ViewModels;

public class ConversationDrawerViewModel : ObservableObject
{
    private readonly ChatViewModel? _parentChatViewModel;

    public ObservableCollection<ChatConversation> Conversations { get; }

    public ICommand SelectConversationCommand { get; }

    public ConversationDrawerViewModel(ChatViewModel? parentChatViewModel)
    {
        _parentChatViewModel = parentChatViewModel;
        Conversations = _parentChatViewModel?.Conversations ?? [];

        SelectConversationCommand = new RelayCommand<ChatConversation>(SelectConversation);
    }

    private void SelectConversation(ChatConversation? conversation)
    {
        if (conversation != null && _parentChatViewModel != null)
        {
            _parentChatViewModel.SelectConversationCommand.Execute(conversation).Subscribe();
        }
    }
}
