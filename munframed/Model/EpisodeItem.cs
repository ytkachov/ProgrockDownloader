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
    private bool _current;

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
        int icount = 0;
        PictureList.Clear();
        var pl = LoadingPictures.Result;
        foreach (var p in pl)
        {
          PictureList.Add(new SongPicture(p));
          if (p.selected)
            icount++;
        }

        PictureList[0].Current = true;
        SelectedImagesCount = icount;
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

    public void PictureSelected(SongPicture sp, bool? isChecked)
    {
      int icount = 0;
      foreach (var p in PictureList)
      {
        if (p.Selected)
          icount++;
      }

      SelectedImagesCount = icount;
    }

    public void PictureClicked(SongPicture sp)
    {
      foreach (var s in PictureList)
        if (sp == s)
          s.Current = true;
        else
          s.Current = false;
    }


    public ObservableCollection<SongPicture> PictureList { get { return _pictures; } private set { _pictures = value; RaisePropertyChanged(); } }
    public bool Current
    {
      get { return _current; }
      set
      {
        _current = value;
        RaisePropertyChanged();

        if (_current && PictureList.Count == 0)
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

    public int SelectedImagesCount
    {
      get { return _song.imagecount; }
      set
      {
        _song.imagecount = value;
        RaisePropertyChanged();
      }
    }
  }
}
