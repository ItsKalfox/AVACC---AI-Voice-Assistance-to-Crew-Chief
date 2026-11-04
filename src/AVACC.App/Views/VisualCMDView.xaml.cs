using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AVACC.App.ViewModels;

namespace AVACC.App.Views;

public partial class VisualCMDView : UserControl
{
    private INotifyCollectionChanged? _eventLogsNotify;

    public VisualCMDView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SubscribeLogs();
        ScrollToBottom();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeLogs();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UnsubscribeLogs();
        SubscribeLogs();
        ScrollToBottom();
    }

    private void SubscribeLogs()
    {
        if (DataContext is VisualCMDViewModel vm && vm.EventLogs is INotifyCollectionChanged notify)
        {
            _eventLogsNotify = notify;
            _eventLogsNotify.CollectionChanged += OnEventLogsCollectionChanged;
        }
    }

    private void UnsubscribeLogs()
    {
        if (_eventLogsNotify != null)
        {
            _eventLogsNotify.CollectionChanged -= OnEventLogsCollectionChanged;
            _eventLogsNotify = null;
        }
    }

    private void OnEventLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
        {
            var newItem = e.NewItems[e.NewItems.Count - 1];
            Dispatcher.InvokeAsync(() =>
            {
                if (newItem != null)
                {
                    EventLogListBox.ScrollIntoView(newItem);
                }
            });
        }
    }

    private void ScrollToBottom()
    {
        if (EventLogListBox.Items.Count > 0)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var last = EventLogListBox.Items[EventLogListBox.Items.Count - 1];
                EventLogListBox.ScrollIntoView(last);
            });
        }
    }

    private void OnHoldToSpeakMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is VisualCMDViewModel vm)
        {
            vm.TriggerPttDownCommand.Execute(null);
        }
    }

    private void OnHoldToSpeakMouseUp(object sender, MouseEventArgs e)
    {
        if (DataContext is VisualCMDViewModel vm)
        {
            vm.TriggerPttUpCommand.Execute(null);
        }
    }
}
