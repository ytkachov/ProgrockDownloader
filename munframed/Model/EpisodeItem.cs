using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using progrock;

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
    private bool _pictures_ready;
    private List<SongPicture> _pictures_list;
    private ObservableCollection<SongPicture> _pictures;

    private NotifyTaskCompletion<List<SongPicture>> _loading_pictures;
    private NotifyTaskCompletion<SongPicture> _picture;

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

    public void FindPictures(PageParser parser)
    {
      _loading_pictures = new NotifyTaskCompletion<List<SongPicture>>(() => parser.FindPicturesAsync(Band, Album, Year));
      _loading_pictures.PropertyChanged += OnPicturesFound;
    }

    public void LoadPicture(SongPicture pict)
    {
      //_picture = new NotifyTaskCompletion<SongPicture>(() => pict.LoadPictureAsync());
      //_picture.PropertyChanged += OnPictureLoaded;
    }

    private void OnPicturesFound(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "IsSuccessfullyCompleted")
      {
        PicturesReady = true;
        _pictures_list = _loading_pictures.Result;
        LoadPictures();
      }
      else
        PicturesReady = false;
    }

    private void LoadPictures()
    {
      //if (_pictures_list.Count > 0)
      //  LoadPicture(_pictures_list[0]);
      //foreach (var pict in _pictures_list)
      //  PictureList.Add(pict);
    }

    private void OnPictureLoaded(object sender, PropertyChangedEventArgs e)
    {
      var sndr = (NotifyTaskCompletion<SongPicture>)sender;
      SongPicture picture = sndr.Result;
      if (e.PropertyName == "IsSuccessfullyCompleted" && picture.Picture != null)
      {
        PictureList.Add(picture);
      }

      if (sndr.IsCompleted)
      {
        int itm = _pictures_list.IndexOf(picture);
        if (itm != -1 && itm != _pictures_list.Count - 1)
          LoadPicture(_pictures_list[itm + 1]);
      }
    }

    public bool PicturesReady { get { return _pictures_ready; } private set { _pictures_ready = value; RaisePropertyChanged(); } }
    public ObservableCollection<SongPicture> PictureList { get { return _pictures; } private set { _pictures = value; RaisePropertyChanged(); } }
    public bool Selected { get { return _selected; } set { _selected = value; RaisePropertyChanged(); } }
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
