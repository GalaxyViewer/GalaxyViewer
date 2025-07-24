using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GalaxyViewer.Models;
using System.Windows.Input;
using Serilog;

namespace GalaxyViewer.ViewModels;

public class ConversationDrawerViewModel : ObservableObject
{
    private readonly ChatViewModel? _parentChatViewModel;
    public event Action? RequestCloseDrawer;

    public ObservableCollection<ChatConversation> Conversations { get; }

    public ICommand SelectConversationCommand { get; }

    public ConversationDrawerViewModel(ChatViewModel? parentChatViewModel)
    {
        Log.Information(
            "ConversationDrawerViewModel created. ParentChatViewModel: {ParentChatViewModel}",
            parentChatViewModel);
        _parentChatViewModel = parentChatViewModel;
        Conversations = _parentChatViewModel?.Conversations ?? [];

        SelectConversationCommand = new RelayCommand<ChatConversation>(SelectConversation);
    }

    private void SelectConversation(ChatConversation? conversation)
    {
        if (conversation == null || _parentChatViewModel == null) return;
        _parentChatViewModel.SelectConversationCommand.Execute(conversation).Subscribe();
        RequestCloseDrawer?.Invoke();
    }
}