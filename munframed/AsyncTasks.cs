using System;
using System.Threading.Tasks;
using System.ComponentModel;

using System.Collections.Generic;

namespace asynctask
{

  // взято отсюда:   https://msdn.microsoft.com/ru-ru/magazine/dn605875.aspx
  // sample task:
  //    public async Task<int> ItemCountAsync()
  //    {
  //      var res = await Task.Run(() =>
  //      {
  //        lock (_lock)
  //        {
  //          return some_long_task();
  //        }
  //      });
  // 
  //      return res;
  //    }

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