using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using progrock;
using asynctask;
using System.Windows.Input;

namespace munframed.model
{
  class EpisodeItem : Notifier
  {
    private episode_item _song;
    private bool _selected;

    private ObservableCollection<SongPicture> _pictures;

    private NotifyTaskCompletion<List<song_picture>> _loading_pictures;
    public NotifyTaskCompletion<List<song_picture>> LoadingPictures { get { return _loading_pictures; } private set { _loading_pictures = value; RaisePropertyChanged(); } }

    public EpisodeItem()
    {
      PictureList = new ObservableCollection<SongPicture>();
    }

    public EpisodeItem(episode_item ei)
    {
      PictureList = new ObservableCollection<SongPicture>();
      _song = ei;
    }

    private void OnPicturesLoaded(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "IsSuccessfullyCompleted")
      {
        PictureList.Clear();
        var pl = LoadingPictures.Result;
        foreach (var p in pl)
          PictureList.Add(new SongPicture(p));
      }
    }

    private void LoadPictures()
    {
      LoadingPictures = new NotifyTaskCompletion<List<song_picture>>(() => LoadPicturesAsync());
      LoadingPictures.PropertyChanged += OnPicturesLoaded;
    }

    private async Task<List<song_picture>> LoadPicturesAsync()
    {
        var res = await Task.Run(() =>
        {
          var lst = new List<song_picture>();

          string [] pictures = _song.FindPictures(podcast.PicturesFolder, false);
          foreach (var pict in pictures)
          {
            var p = new song_picture(pict);
            lst.Add(p);
          }

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

        if (_selected && PictureList.Count == 0)
          LoadPictures();
      }
    }

    public string Name 
    {
      get { return _song.name; }
      set
      {
        _song.name = value;
        RaisePropertyChanged();
      }
    }
    public string Band
    {
      get { return _song.band; }
      set
      {
        _song.band = value;
        RaisePropertyChanged();
      }
    }
    public string Album
    {
      get { return _song.album; }
      set
      {
        _song.album = value;
        RaisePropertyChanged();
      }
    }
    public int Year
    {
      get { return _song.year; }
      set
      {
        _song.year = value;
        RaisePropertyChanged();
      }
    }
    public string Start
    {
      get { return _song.start; }
      set
      {
        _song.start = value;
        RaisePropertyChanged();
      }
    }
    public string Duration
    {
      get { return _song.duration; }
      set
      {
        _song.duration = value;
        RaisePropertyChanged();
      }
    }

    private void refresh_images()
    {

    }
  }
}
