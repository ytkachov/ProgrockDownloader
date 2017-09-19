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
    private CommandHandler _go_prev_picture;
    private CommandHandler _go_next_picture;
    private CommandHandler _toggle_current_picture_selection;

    private int _current_episode;
    private podcast _podcast;

    #region Constructor

    public MusicUnframed(Shell view)
    {
      _go_next = new CommandHandler(() => GoTo(true), false);
      _go_prev = new CommandHandler(() => GoTo(false), false);
      _go_next_song = new CommandHandler(() => GoSong(true), true);
      _go_prev_song = new CommandHandler(() => GoSong(false), true);
      _go_next_picture = new CommandHandler(() => GoPicture(true), true);
      _go_prev_picture = new CommandHandler(() => GoPicture(false), true);
      _toggle_current_picture_selection = new CommandHandler(() => ToggleCurrentPictureSelection(), true);

      _view = view;
      EpisodeItems = new ObservableCollection<EpisodeItem>();

      podcast.RootFolder = @"Y:\ProgRock";
      _podcast = podcast.create(podcast.PodcastType.MusicUnframed, _podcastfilename);
      _podcast.mark_repeats();

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

      EpisodeItems.Clear();
      foreach (var ei in _podcast.Episodes[_current_episode].Items)
        if (!ei.albumrepeated)
          EpisodeItems.Add(new EpisodeItem(ei));

      CurrentEpisodeItem = EpisodeItems[0];
      CurrentEpisodeItem.Current = true;

      ScrollViewer sl = FindChild<ScrollViewer>(_view, "EpisodeItemList");
      if (sl != null)
        sl.ScrollToVerticalOffset(0);

      ScrollViewer pl = FindChild<ScrollViewer>(_view, "SongPictureList");
      if (pl != null)
        pl.ScrollToVerticalOffset(0);

    }
    #endregion

    public void EpisodeItemClicked(EpisodeItem ei)
    {
      CurrentEpisodeItem = ei;
    }

    private string _ep_title;
    private int _ep_item_count;
    private ObservableCollection<EpisodeItem> _song_list;
    private EpisodeItem _current_item;

    public int ItemCount { get { return _ep_item_count; } private set { _ep_item_count = value; RaisePropertyChanged(); } }
    public string Title { get { return _ep_title; } private set { _ep_title = value; RaisePropertyChanged(); } }
    public ObservableCollection<EpisodeItem> EpisodeItems { get { return _song_list; } private set { _song_list = value; RaisePropertyChanged(); } }
    public EpisodeItem CurrentEpisodeItem
    {
      get { return _current_item; }
      private set
      {
        if (_current_item != null)
          _current_item.Current = false;

        _current_item = value;
        SetCurrentItem();

        RaisePropertyChanged();
      }
    }

    private void SetCurrentItem()
    {
      if (_current_item == null)
        return;

      for (int i = 0; i < EpisodeItems.Count; i++)
      {
        var ei = EpisodeItems[i];
        if (ei == _current_item)
        {
          ei.Current = true;

          var EpisodeItemsHost = FindChild<ItemsControl>(_view, "EpisodeItemsHost");
          var econtainer = EpisodeItemsHost.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
          if (econtainer != null)
            econtainer.BringIntoView();

          // if (ei.PictureList.Count == 0)
          {
            ScrollViewer pl = FindChild<ScrollViewer>(_view, "SongPictureList");
            if (pl != null)
              pl.ScrollToVerticalOffset(0);
          }
          //else
          {
            //int cp = -1, sp = -1;
            //for (int j = 0; j < ei.PictureList.Count; j++)
            //{
            //  if (ei.PictureList[j].Current)
            //    cp = j;
            //  if (ei.PictureList[j].Selected)
            //    sp = j;
            //}
            //int p = 0;
            //if (cp != -1)
            //  p = cp;
            //else if (sp != -1)
            //  p = sp;

            //var PictureListHost = FindChild<ItemsControl>(_view, "PictureListHost");
            //var pcontainer = PictureListHost.ItemContainerGenerator.ContainerFromIndex(p) as FrameworkElement;
            //if (pcontainer != null)
            //  pcontainer.BringIntoView();
          }
          break;
        }
      }
    }

    private void Initialize(string url)
    {
      CurrentEpisodeItem = null;
    }

    public ICommand GoPrev { get { return _go_prev; } }
    public ICommand GoNext { get { return _go_next; } }
    public ICommand PrevSong { get { return _go_prev_song; } }
    public ICommand NextSong { get { return _go_next_song; } }
    public ICommand PrevPicture { get { return _go_prev_picture; } }
    public ICommand NextPicture { get { return _go_next_picture; } }
    public ICommand ToggleSelection { get { return _toggle_current_picture_selection; } }

    private void GoTo(bool next = true)
    {
      SetEpisode(_current_episode + (next ? +1 : -1));
    }

    private void GoSong(bool next = true)
    {
      for (int i = 0; i < EpisodeItems.Count; i++)
      {
        var ei = EpisodeItems[i];
        if (ei.Current)
        {
          int ni = i + (next ? 1 : -1);
          if (ni >= 0 && ni < EpisodeItems.Count)
          {
            CurrentEpisodeItem = EpisodeItems[ni];
            break;
          }
        }
      }
    }

    private void GoPicture(bool next = true)
    {
      var PictureList = CurrentEpisodeItem.PictureList;
      for (int i = 0; i < PictureList.Count; i++)
      {
        var cp = PictureList[i];
        if (cp.Current)
        {
          int ni = i + (next ? 1 : -1);
          if (ni >= 0 && ni < PictureList.Count)
          {
            cp.Current = false;
            PictureList[ni].Current = true;
            var PictureListHost = FindChild<ItemsControl>(_view, "PictureListHost");

            var container = PictureListHost.ItemContainerGenerator.ContainerFromIndex(ni) as FrameworkElement;
            container.BringIntoView();

            break;
          }
        }
      }
    }

    private void ToggleCurrentPictureSelection()
    {
      var PictureList = CurrentEpisodeItem.PictureList;
      for (int i = 0; i < PictureList.Count; i++)
      {
        var cp = PictureList[i];
        if (cp.Current)
        {
          cp.Selected = !cp.Selected;
          break;
        }
      }
    }

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

  }
}

