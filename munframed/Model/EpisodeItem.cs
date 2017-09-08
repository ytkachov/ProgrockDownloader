using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using progrock;
using asynctask;

namespace munframed.model
{
  class EpisodeItem : Notifier
  {
    private string _name;
    private string _band;
    private string _album;
    private int _year;
    private string _start;
    private string _duration;
    private bool _selected;

    private List<SongPicture> _pictures_list;
    private ObservableCollection<SongPicture> _pictures;

    private NotifyTaskCompletion<List<SongPicture>> _loading_pictures;
    public NotifyTaskCompletion<List<SongPicture>> LoadingPictures { get { return _loading_pictures; } private set { _loading_pictures = value; RaisePropertyChanged(); } }

    public EpisodeItem()
    {
      PictureList = new ObservableCollection<SongPicture>();
    }

    public EpisodeItem(episode_item ei)
    {
      PictureList = new ObservableCollection<SongPicture>();

      Name = ei.name;
      Band = ei.band;
      Album = ei.album;
      Year = ei.year;
      Start = ei.start;
      Duration = ei.duration;
    }

    private void OnPicturesLoaded(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "IsSuccessfullyCompleted")
      {
        _pictures_list = LoadingPictures.Result;
        PictureList = new ObservableCollection<SongPicture>(_pictures_list);
      }
    }

    private void LoadPictures()
    {
      LoadingPictures = new NotifyTaskCompletion<List<SongPicture>>(() => LoadPicturesAsync());
      LoadingPictures.PropertyChanged += OnPicturesLoaded;
    }

    private async Task<List<SongPicture>> LoadPicturesAsync()
    {
        var res = await Task.Run(() =>
        {
          var lst = new List<SongPicture>();


          return lst;
        });
        
        return res;
    }

    public ObservableCollection<SongPicture> PictureList { get { return _pictures; } private set { _pictures = value; RaisePropertyChanged(); } }
    public bool Selected
    {
      get { return _selected; }
      set
      {
        _selected = value;
        RaisePropertyChanged();

        if (PictureList.Count == 0)
          LoadPictures();
      }
    }

    public string Name 
    {
      get { return _name; }
      set
      {
        _name = value;
        RaisePropertyChanged();
      }
    }
    public string Band
    {
      get { return _band; }
      set
      {
        _band = value;
        RaisePropertyChanged();
      }
    }
    public string Album
    {
      get { return _album; }
      set
      {
        _album = value;
        RaisePropertyChanged();
      }
    }
    public int Year
    {
      get { return _year; }
      set
      {
        _year = value;
        RaisePropertyChanged();
      }
    }
    public string Start
    {
      get { return _start; }
      set
      {
        _start = value;
        RaisePropertyChanged();
      }
    }
    public string Duration
    {
      get { return _duration; }
      set
      {
        _duration = value;
        RaisePropertyChanged();
      }
    }

  }
}
