using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
