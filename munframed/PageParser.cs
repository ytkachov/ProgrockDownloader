using System;
using System.Threading.Tasks;
using System.ComponentModel;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome; //to use googlechrome browser.
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Support.UI;
using System.Collections.Generic;
using munframed.model;

namespace munframed
{
  internal class PageParser
  {
    private IWebDriver _driver = new ChromeDriver();


    public PageParser()
    {
      _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(20);
    }

    internal void Shutdown()
    {
      _driver.Close();
      _driver.Quit();
    }


    public async Task<string> Exists(string url)
    {
      try
      {
        _driver.Navigate().GoToUrl(url);
      }
      catch
      {
        throw new PageTimeoutException();
      }

      var hdr = _driver.findElement(By.XPath("//*[@id=\"masthead\"]/div/div/h1/a"));
      if (hdr == null || hdr.Text != "Music Unframed")
        throw new WrongSiteException();

      var oops = _driver.findElement(By.XPath("//*[@id=\"main\"]/section/header/h1"));
      if (oops != null && oops.Text == "Oops! That page can’t be found.")
        throw new WrongPageException();

      return _driver.Title;
    }

    public async Task<int> ItemCount()
    {
      var tbl = _driver.findElement(By.XPath("//*[@id=\"dslc-theme-content-inner\"]/table"));
      if (tbl == null)
        throw new IncorrectPageStructureException("Table of contents not found");

      var items = tbl.FindElements(By.TagName("tr"));
      return items.Count - 1;
    }

    public async Task<Tuple<string, string>> PrevPage()
    {
      var link = _driver.findElement(By.ClassName("nav-previous")).findElement(By.XPath("./a"));
      if (link == null)
        throw new LinkNotFoundException("No previous episode found");

      return new Tuple<string, string>(link.Text, link.GetAttribute("href"));
    }

    public async Task<Tuple<string, string>> NextPage()
    {
      var link = _driver.findElement(By.ClassName("nav-next")).findElement(By.XPath("./a"));
      if (link == null)
        throw new LinkNotFoundException("No next episode found");

      return new Tuple<string, string>(link.Text, link.GetAttribute("href"));
    }

    public async Task<List<Tuple<string, string, string, string, string, string>>> TOC()
    {
      var toc = new List<Tuple<string, string, string, string, string, string>>();

      var tbl = _driver.findElement(By.XPath("//*[@id=\"dslc-theme-content-inner\"]/table"));
      if (tbl == null)
        throw new IncorrectPageStructureException("Table of contents not found");

      var items = tbl.FindElements(By.TagName("tr"));
      for (int i = 1; i < items.Count; i++)
      {
        var tds = items[i].FindElements(By.TagName("td"));
        if (tds.Count == 4)
          toc.Add(new Tuple<string, string, string, string, string, string>(tds[0].Text, tds[1].Text, tds[2].Text, "", tds[3].Text, "0"));
        else 
          toc.Add(new Tuple<string, string, string, string, string, string>(tds[0].Text, tds[1].Text, tds[2].Text, tds[3].Text, tds[4].Text, tds[5].Text));
      }

      return toc;
    }
  }

  internal class WrongSiteException : Exception { public WrongSiteException() : base("URL provided is not Music Unframed Site") { } }
  internal class WrongPageException : Exception { public WrongPageException() : base("Page not found on the site") { } }
  internal class PageTimeoutException : Exception { public PageTimeoutException() : base("Can't load the requested page") { } }
  internal class LinkNotFoundException : Exception { public LinkNotFoundException(string msg) : base(msg) { } }
  internal class IncorrectPageStructureException : Exception { public IncorrectPageStructureException(string msg) : base(msg) { } }

  // взято отсюда:   https://msdn.microsoft.com/ru-ru/magazine/dn605875.aspx
  public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
  {
    public NotifyTaskCompletion(Task<TResult> task)
    {
      Task = task;
      if (!Task.IsCompleted)
      {
        var _ = WatchTaskAsync(Task);
      }
    }

    private async Task WatchTaskAsync(Task task)
    {
      try
      {
        await task;
      }
      catch
      {
      }

      var propertyChanged = PropertyChanged;
      if (propertyChanged == null)
        return;

      propertyChanged(this, new PropertyChangedEventArgs("Status"));
      propertyChanged(this, new PropertyChangedEventArgs("IsCompleted"));
      propertyChanged(this, new PropertyChangedEventArgs("IsNotCompleted"));

      if (task.IsCanceled)
      {
        propertyChanged(this, new PropertyChangedEventArgs("IsCanceled"));
      }
      else if (task.IsFaulted)
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

    public Task<TResult> Task { get; private set; }
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

  public static class SeleniumExtensions
  {
    public static IWebElement findElement(this ISearchContext self, By by)
    {
      if (self == null)
        return null;

      IWebElement el = null;
      try
      {
        el = self.FindElement(by);
      }
      catch
      {
      }

      return el;
    }
  }

}