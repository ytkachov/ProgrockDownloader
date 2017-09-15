using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using progrock;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace munframed.model
{
  public class CommandHandler : ICommand
  {
    private Action _action;
    private bool _canExecute;
    public CommandHandler(Action action, bool canExecute)
    {
      _action = action;
      _canExecute = canExecute;
    }

    public bool Active
    {
      set
      {
        _canExecute = value;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public bool CanExecute(object parameter)
    {
      return _canExecute;
    }

    public event EventHandler CanExecuteChanged;
    public void Execute(object parameter)
    {
      _action();
    }
  }

  class MusicUnframed : Notifier
  {
    private Shell _view;

    private static string _podcastfilename = "munframed.xml";

    private CommandHandler _go_prev;
    private CommandHandler _go_next;
    private CommandHandler _go_prev_song;
    private CommandHandler _go_next_song;

    private int _current_episode;
    private podcast _podcast;

    #region Constructor

    public MusicUnframed(Shell view)
    {
      _go_next = new CommandHandler(() => GoTo(true), false);
      _go_prev = new CommandHandler(() => GoTo(false), false);
      _go_next_song = new CommandHandler(() => GoSong(true), true);
      _go_prev_song = new CommandHandler(() => GoSong(false), true);

      _view = view;
      SongList = new ObservableCollection<EpisodeItem>();

      podcast.RootFolder = @"Y:\ProgRock";
      _podcast = podcast.create(podcast.PodcastType.MusicUnframed, _podcastfilename);

      int idx = 0;
      for (int i = 0; i < _podcast.Episodes.Count; i++)
        if (_podcast.Episodes[i].Current)
        {
          idx = i;
          break;
        }

      SetEpisode(idx);
    }

    public void Save()
    {
      _podcast.save();
    }

    private void SetEpisode(int idx)
    {
      _podcast.Episodes[_current_episode].Current = false;
      _podcast.Episodes[idx].Current = true;

      _current_episode = idx;
      _go_prev.Active = true;
      _go_next.Active = true;
      if (_current_episode == 0)
        _go_prev.Active = false;

      if (_current_episode == _podcast.Episodes.Count - 1)
        _go_next.Active = false;

      Title = _podcast.Episodes[_current_episode].Name;
      ItemCount = _podcast.Episodes[_current_episode].Items.Count;

      SongList.Clear();
      foreach (var ei in _podcast.Episodes[_current_episode].Items)
        SongList.Add(new EpisodeItem(ei));

      SelectedItem = SongList[0];
      SelectedItem.Selected = true;

      ScrollViewer sl = FindChild<ScrollViewer>(_view, "EpisodeItemList");
      if (sl != null)
        sl.ScrollToVerticalOffset(0);

      ScrollViewer pl = FindChild<ScrollViewer>(_view, "SongPictureList");
      if (pl != null)
        pl.ScrollToVerticalOffset(0);

    }
    #endregion

    public static T FindChild<T>(DependencyObject parent, string childName)
       where T : DependencyObject
    {
      // Confirm parent and childName are valid. 
      if (parent == null) return null;

      T foundChild = null;

      int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
      for (int i = 0; i < childrenCount; i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        // If the child is not of the request child type child
        T childType = child as T;
        if (childType == null)
        {
          // recursively drill down the tree
          foundChild = FindChild<T>(child, childName);

          // If the child is found, break so we do not overwrite the found child. 
          if (foundChild != null) break;
        }
        else if (!string.IsNullOrEmpty(childName))
        {
          var frameworkElement = child as FrameworkElement;
          // If the child's name is set for search
          if (frameworkElement != null && frameworkElement.Name == childName)
          {
            // if the child's name is of the request name
            foundChild = (T)child;
            break;
          }
        }
        else
        {
          // child element found.
          foundChild = (T)child;
          break;
        }
      }

      return foundChild;
    }
    public void EpisodeItemClicked(EpisodeItem ei)
    {
      foreach (var e in SongList)
        e.Selected = false;

      ei.Selected = true;
      SelectedItem = ei;
    }

    private string _ep_title;
    private int _ep_item_count;
    private ObservableCollection<EpisodeItem> _song_list;
    private EpisodeItem _selected_item;

    public int ItemCount { get { return _ep_item_count; } private set { _ep_item_count = value; RaisePropertyChanged(); } }
    public string Title { get { return _ep_title; } private set { _ep_title = value; RaisePropertyChanged(); } }
    public ObservableCollection<EpisodeItem> SongList { get { return _song_list; } private set { _song_list = value; RaisePropertyChanged(); } }
    public EpisodeItem SelectedItem { get { return _selected_item; } private set { _selected_item = value; RaisePropertyChanged(); } }

    private void Initialize(string url)
    {
      SelectedItem = null;
    }

    public ICommand GoPrev { get { return _go_prev; } }
    public ICommand GoNext { get { return _go_next; } }
    public ICommand PrevSong { get { return _go_prev_song; } }
    public ICommand NextSong { get { return _go_next_song; } }

    private void GoTo(bool next = true)
    {
      SetEpisode(_current_episode + (next ? +1 : -1));
    }

    private void GoSong(bool next = true)
    {
      for (int i = 0; i < SongList.Count; i++)
      {
        var ei = SongList[i];
        if (ei.Selected)
        {
          int ni = i + (next ? 1 : -1);
          if (ni >= 0 && ni < SongList.Count)
          {
            ei.Selected = false;
            SongList[ni].Selected = true;
            SelectedItem = SongList[ni];
            var SongListHost = FindChild<ItemsControl>(_view, "SongListHost");

            var container = SongListHost.ItemContainerGenerator.ContainerFromIndex(ni) as FrameworkElement;
            container.BringIntoView();

            break;
          }
        }
      }
    }
  }
}

