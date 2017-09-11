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

    private int _current_episode;
    private podcast _podcast;

    #region Constructor

    public MusicUnframed(Shell view)
    {
      _go_next = new CommandHandler(() => GoTo(true), false);
      _go_prev = new CommandHandler(() => GoTo(false), false);

      _view = view;
      SongList = new ObservableCollection<EpisodeItem>();

      podcast.RootFolder = @"Y:\ProgRock";
      _podcast = podcast.create("MusicUnframed", _podcastfilename);

      SetEpisode(0);
    }

    private void SetEpisode(int idx)
    {
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

    }
    #endregion

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

    private void GoTo(bool next = true)
    {
      SetEpisode(_current_episode += (next ? +1 : -1));
    }

  }
}

