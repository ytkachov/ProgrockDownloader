using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace munframed.model
{
  class MusicUnframed : Notifier
  {
    private string _source_path;
    private Shell _view;

    #region Constructor

    public MusicUnframed(Shell view)
    {
      _view = view;
      SongList = new ObservableCollection<EpisodeItem>();
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
    private ObservableCollection<EpisodeItem> _song_list;
    private EpisodeItem _selected_item;

    public string Title { get { return _ep_title; } private set { _ep_title = value; RaisePropertyChanged(); } }
    public ObservableCollection<EpisodeItem> SongList { get { return _song_list; } private set { _song_list = value; RaisePropertyChanged(); } }
    public EpisodeItem SelectedItem { get { return _selected_item; } private set { _selected_item = value; RaisePropertyChanged(); } }

    private void Initialize(string url)
    {
      SelectedItem = null;
    }

    public void GoTo(bool next = true)
    {
    }
  }
}

