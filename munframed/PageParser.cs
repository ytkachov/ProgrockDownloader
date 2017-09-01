using System;
using System.Threading.Tasks;
using System.ComponentModel;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome; //to use googlechrome browser.
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Support.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using progrock;
using munframed.model;

namespace munframed
{
  internal class PageParser : page_parser

  {
    private object _lock = new object();

    public async Task<string> ExistsAsync(string url)
    {
      var res = await Task.Run(() =>
      {
        lock (_lock)
        {
          return Exists(url);
        }
      });

      return res;
    }

    public async Task<int> ItemCountAsync()
    {
      var res = await Task.Run(() =>
      {
        lock (_lock)
        {
          return ItemCount();
        }
      });

      return res;
    }

    public async Task<Tuple<string, string>> PrevPageAsync()
    {
      var res = await Task.Run(() =>
      {
        lock (_lock)
        {
          return PrevPage();
        }
      });

      return res;
    }

    public async Task<Tuple<string, string>> NextPageAsync()
    {
      var res = await Task.Run(() =>
      {
        lock (_lock)
        {
          return NextPage();
        }
      });

      return res;
    }

    public async Task<List<EpisodeItem>> TOCAsync()
    {
      var res = await Task.Run(() =>
      {
        lock (_lock)
        {
          var eis = TOC();
          var eitems = new List<EpisodeItem>();
          foreach (var ei in eis)
            eitems.Add(new EpisodeItem(ei));

          return eitems;
        }
      });

      return res;
    }

    public async Task<List<SongPicture>> FindPicturesAsync(string band, string album, int year)
    {
      var res = await Task.Run(() =>
      {
        lock (_lock)
        {
          var picts = FindPictures(band, album, year);

          var pictures = new List<SongPicture>();
          foreach (var pict in picts)
            pictures.Add(new SongPicture(pict));

          return pictures;
        }
      });

      return res;
    }

  }

  // взято отсюда:   https://msdn.microsoft.com/ru-ru/magazine/dn605875.aspx
  public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
  {
    public Task<TResult> Task { get; private set; }
    public NotifyTaskCompletion(Func<Task<TResult>> task)
    {
      if (Task == null)
      {
        TaskCompletion = WatchTask(task);
      }
    }

    public Task TaskCompletion { get; set; }

    private async Task WatchTask(Func<Task<TResult>> task)
    {
      try
      {
        Task = task();
        await Task;
      }
      catch
      {

      }

      OnTaskCompletion();
    }

    private void OnTaskCompletion()
    {
      var propertyChanged = PropertyChanged;
      if (propertyChanged == null)
        return;

      propertyChanged(this, new PropertyChangedEventArgs("Status"));
      propertyChanged(this, new PropertyChangedEventArgs("IsCompleted"));
      propertyChanged(this, new PropertyChangedEventArgs("IsNotCompleted"));

      if (Task.IsCanceled)
      {
        propertyChanged(this, new PropertyChangedEventArgs("IsCanceled"));
      }
      else if (Task.IsFaulted)
      {
        propertyChanged(this, new PropertyChangedEventArgs("IsFaulted"));
        propertyChanged(this, new PropertyChangedEventArgs("Exception"));
        propertyChanged(this, new PropertyChangedEventArgs("InnerException"));
        propertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
      }
      else
      {
        propertyChanged(this, new PropertyChangedEventArgs("IsSuccessfullyCompleted"));
        propertyChanged(this, new PropertyChangedEventArgs("Result"));
      }
    }

    public TResult Result
    {
      get
      {
        return (Task.Status == TaskStatus.RanToCompletion) ? Task.Result : default(TResult);
      }
    }

    public TaskStatus Status { get { return Task.Status; } }
    public bool IsCompleted { get { return Task.IsCompleted; } }
    public bool IsNotCompleted { get { return !Task.IsCompleted; } }
    public bool IsSuccessfullyCompleted
    {
      get
      {
        return Task.Status == TaskStatus.RanToCompletion;
      }
    }

    public bool IsCanceled { get { return Task.IsCanceled; } }
    public bool IsFaulted { get { return Task.IsFaulted; } }
    public AggregateException Exception { get { return Task.Exception; } }

    public Exception InnerException
    {
      get
      {
        return (Exception == null) ? null : Exception.InnerException;
      }
    }
    public string ErrorMessage
    {
      get
      {
        return (InnerException == null) ? null : InnerException.Message;
      }
    }
    public event PropertyChangedEventHandler PropertyChanged;
  }

}