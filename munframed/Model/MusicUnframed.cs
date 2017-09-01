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
    private bool _can_go_prev = true;
    private ICommand _go_prev;
    private bool _can_go_next = true;
    private ICommand _go_next;
    private string _source_url;

    private PageParser _parser = new PageParser();
    private Shell _view;

    #region Constructor

    public MusicUnframed(Shell view)
    {
      _view = view;

      SongList = new ObservableCollection<EpisodeItem>();
      SourceUrl = @"http://munframed.com/episode1";
    }

    #endregion

    public string SourceUrl
    {
      get { return _source_url; }
      set
      {
        _source_url = value;

        SongList.Clear();

        RaisePropertyChanged();
        Initialize(_source_url);
      }
    }

    internal void Shutdown()
    {
      _parser.Shutdown();
    }

    public void EpisodeItemClicked(EpisodeItem ei)
    {
      foreach (var e in SongList)
        e.Selected = false;

      ei.Selected = true;
      SelectedItem = ei;
    }

    private NotifyTaskCompletion<string> _exists;
    private NotifyTaskCompletion<int> _item_count;
    private NotifyTaskCompletion<Tuple<string, string>> _prev_episode;
    private NotifyTaskCompletion<Tuple<string, string>> _next_episode;
    private NotifyTaskCompletion<List<EpisodeItem>> _toc;

    private ObservableCollection<EpisodeItem> _song_list;
    private EpisodeItem _selected_item;

    public NotifyTaskCompletion<Tuple<string, string>> PrevEpisode { get { return _prev_episode; } private set { _prev_episode = value; RaisePropertyChanged(); } }
    public NotifyTaskCompletion<Tuple<string, string>> NextEpisode { get { return _next_episode; } private set { _next_episode = value; RaisePropertyChanged(); } }
    public NotifyTaskCompletion<int> ItemCount { get { return _item_count; } private set { _item_count = value; RaisePropertyChanged(); } }
    public NotifyTaskCompletion<string> Exists  { get { return _exists; } private set { _exists = value; RaisePropertyChanged(); } }
    public NotifyTaskCompletion<List<EpisodeItem>> TOC { get { return _toc; } private set { _toc = value; RaisePropertyChanged(); } }

    public ObservableCollection<EpisodeItem> SongList { get { return _song_list; } private set { _song_list = value; RaisePropertyChanged(); } }
    public EpisodeItem SelectedItem { get { return _selected_item; } private set { _selected_item = value; RaisePropertyChanged(); } }

    private void Initialize(string url)
    {
      SelectedItem = null;

      Exists = new NotifyTaskCompletion<string>(() => _parser.ExistsAsync(url));
      Exists.PropertyChanged += OnExistsSuccessfullyCompleted;
    }

    private void OnExistsSuccessfullyCompleted(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "IsSuccessfullyCompleted")
      {
        ItemCount = new NotifyTaskCompletion<int>(() => _parser.ItemCountAsync());
        PrevEpisode = new NotifyTaskCompletion<Tuple<string, string>>(() => _parser.PrevPageAsync());
        NextEpisode = new NotifyTaskCompletion<Tuple<string, string>>(() => _parser.NextPageAsync());
        TOC = new NotifyTaskCompletion<List<EpisodeItem>>(() => _parser.TOCAsync());

        TOC.PropertyChanged += OnTOCCompleted;
      }
    }

    private void OnTOCCompleted(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "IsSuccessfullyCompleted")
      {
        SongList.Clear();
        foreach (var ei in TOC.Result)
          SongList.Add(ei);

        SelectedItem = SongList[0];
        SongList[0].PropertyChanged += OnPicturesReady;
        SongList[0].FindPictures(_parser);
      }
    }

    private void OnPicturesReady(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName != "PicturesReady")
        return;

      int itm = SongList.IndexOf((EpisodeItem)sender);
      if (itm != -1)
      {
        SongList[itm].PropertyChanged -= OnPicturesReady;

        // go to next song
        if (itm != SongList.Count - 1)
        {
          SongList[itm + 1].PropertyChanged += OnPicturesReady;
          SongList[itm + 1].FindPictures(_parser);
        }
      }
    }

    public ICommand GoPrev
    {
      get
      {
        return _go_prev ?? (_go_prev = new CommandHandler(() => GoTo(false), _can_go_prev));
      }
    }

    public ICommand GoNext
    {
      get
      {
        return _go_next ?? (_go_next = new CommandHandler(() => GoTo(true), _can_go_next));
      }
    }

    public void GoTo(bool next = true)
    {

      //src="data:image/jpeg;base64,/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAkGBxMTEhUTExMVFRUXFhoYGBYYGBcYFxcYGBgYGRoYFxodHyggGB0lGxgVITEhJSkrLi4uFx8zODMtNygtLisBCgoKDg0OGxAQGi0gHyUtLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLf/AABEIAOEA4AMBIgACEQEDEQH/xAAcAAABBAMBAAAAAAAAAAAAAAAAAwQFBgIHCAH/xABGEAABAwIDBAcFBQUGBgMBAAABAgMRAAQSITEFQVFhBhMicYGRoQcyQrHBFCNSgtFDYpLh8AgVcqKy8SQzU1RjczREZBb/xAAZAQADAQEBAAAAAAAAAAAAAAAAAgMBBAX/xAAiEQADAQADAQACAgMAAAAAAAAAAQIRAyExEkFRIjITYfD/2gAMAwEAAhEDEQA/AN40UUUAFFFFABRRRQAUUVT+lntFs7IlBUXnh
      //     +yaglP/ALFaI7teRoMbwuFQu3uldlZj/ibltsxOCZWe5CZUfKtE9JvaVtG7lKV/Zmj8DJIUR+877x/LhHKqtZ7FLhKlKEYpVJ7StFLg7yAZzqk8bYj5Ejb22fbpbIkW1s68fxLIaQeY95XmkVTto+2/aKyeqRbsjdCVLUPFSoP8Na+cb1jTcOHKs7XZ5WFqkJCEqOfxFKSrCPAE8qFHeB9E9c+0zay9b1wf4Eto/wBKQaj3OmW0VGTf3fg84PkRUNFeYaMDSaa6YbRTmL+78X3FehJqStfaTtVGl64Y3KS2ueRxJquP2hQlClZFckD90RBPCTPlSaGTW/Jn0bJ2V7aNpBSUrQw/JiChSFEngUqj/LVz2V7a2DAurV1nipBDqRuk+6rwANaHSiDT26ccCUIUqRmpKZMAK3xoJj0p1E52b9nUuweltlef/HuW1q/BOFwd6FQoeVTdcZq47xmDvHdVy6Me1DaFoQkufaWh8DxKiB+657w8cQHCkfH+jVR01RVM6Ie0mzvoTJYeO
      //     XVOwMR/8a/dX3ZHlVzqbTXo4UUUVgBRRRQAUUUUAFFFFABRRRQAUx2ztdm1aU9cOJbbTqTvO4JAzUo7gMzUX016YW+zmeseMrVIbaT77iuXBI3qOQ7yAecOlHSe42g911wrIe42J6tscEjjxUcz5ANM6LVYW/px7U7m6lq1xW7GhUDDzg5qH/LHJJnnuqiWp0Eb5pFKppZnKrKUjnqm/R+21JEAzwAml7rstnCFIKViSdSFpUkmIy4Vih4dmRIKD/FmPQx6UuZU2oE5YAAPzpI9Zq0roi32V9TBgwNNazubnA0loJGJSSVKkyA4fdA0zQlGfAkVNX6ShCwjCodjI5jMEyU6TIGv6UwU4px1xBUISk9WkBCJUmE5KgQcOKJ+lK4wtNaQamyMiIPA5GlbVkFQnTU9yRJ9ARWd26pxalGJPAzoABnvyGu+nyrktykMtpOkKSFHDocSjmZPy7qmkhtGt8gy3P8A00nxXKz6qNKliEJUTBOQTnJEkYh4iIp3eWynCyUhICmwMoSAU
      //     5KBPLKlGcRDCBKVFSkKIyVhxAwDqPePlVFPbF0jFWa8uyRJAEgj3pj5HyrO+aJccIHZScIyyAT2R6AVL27ai43i1K1vKHCJwp8MJEc6SZYVhSFnslRURxDYKiAOcqk8hyo+A+iLZsVq0HxBIGkkzHyOdD9qUhJMSqTA3AGJPeZ8qlW3f2m6XHCDxCQEju6xZpncoOJI3pbR5kYz6qNbiSDRBElOEk4dY3THD61fOhXtQubTC1cYri30zMvNj9xR98fuqPcRpVJUjTKsHgUgmKKxrGMqOrdibZYu2g9buBxB3jUH8Kgc0qHA1IVzj0M20pkddbDqXkKSHEBSlNXCFYveSomCCBmDlOUb96dGekTd43iT2Vj32zqk8vxJO4/I5Vz3xNLV4Um96/JNUUUVIoFFFFABRRRQAVXum/S1nZ1sXne0o9lpoGFOL4DgBqVbhzgGU21tVq1YcuHlBLbacSj6ADiSSABvJFcr9Mekru0blT7pgZhtvc23OSRxO8neeUANM6LVYM9ubXfvX
      //  1XFwvG4rwSlI0QgfCkcPEySTSTLRrKzAGudSpaEAiJO6uiZOeq7GKGacNszSpA7qySRT4JoIQoDQETodQeI4fKl+sGmgnv8+NKsLSEqxAkxCY45a8omo4kzWvoT0dvXCOwAkTIk1BvEKJJGZM07UqJPAH1y+tNm25paejysMG26c9XJk5k+JpRpmn1szmBFYpB0e2bq2wEITixQcyYGIRlGYy3zXimgVIwJKOzMSeySTodeB8alUNYSN0CAf1rzDh5mq50T+xku1IO/IBIjXIZ+s+dIotyM+RBPAKEZeBNSYxK3Vmm3jWczMf13mt+TPsgXLeBBJ4EetKXrUPL5RHdhEelSNzZknTu591R/2Ygn+jS5g6rTItjcCTMgnLLhHlvpdhqTmmhtgjOpG3TlzppQjoW2W1hMISB4Zd9SY2q7brDrasKwZmMjxCuKTw/lSFi5gOYBpveIk5me+rdZgKjdfRHpM1fM40ZLTk42dUn6pOoP1Bqdrm/ZO3HLJ9D7J93JSNA4jelX0O4gGugt
      //  ibWaumEPsqxIWJHEHQpVwIIII4ivN5eP4fXh3cd/SH1FFFSKBRRVS9p3Sj7BZLWkw84era5KUDKvypCld4A31qWgzWPtk6UfbHlWbKpbt19oA5OOgEK7wj3RnriyOVatSjOnrBGoPfOvnvrx+MUjfXSpxHK6bZ601lmRTxpxOE5mdwpm2ql0kCmQrHPWTlTgMRnSNugE0/cuMOUZaCd/6U6FZgsQmY1psEGJFPzcoUnDh9aUSUpSMIVOcyN2W+mwTSFvVhRISjBJk5yJ5cBnpSSG4qcVs/F2tJnIcRypt9mE0jlm/Q2tgQNNalrS3JIpxZWenZkVKtWvCnUkqoT+y7jHdSLtpFS6Goy31K21qjCQoYiRkeEU+E9Klb26grI5g7jTssBapVJO8z+utSt3bZ9kQI/rOgWJj61uGaML2yCUgojM676if7vUTpv36eNXK1QkJwLTI3GdNKQuGAJHGtc6CrCtI2eRqJnSKT6gg5bt9TFwkhUjQUhjzzGtZhv0xm6QnPdxmKYvvknsyZ8
      //  Y11qQvnDrx1ioo3UgJMwPM99Y2PJEXbsmrd7Iulptbr7M4fuH1ACdEOnJKu5WST+U7jVS2u8hABSDJ3H5magL66xGBkBpGXjXPyZ4zq499Oy6Kpfsn6V/b7FJWZfZPVO8SQOyv8yc+8K4VdK4zsCucfbJ0j+036mkmW7aWhw6zV1Q54gE/krfPSna4tLR+5P7JtSgOKohI8VEDxrkcrKiVKJUoklROpJzJPeZqnGu9J8j6wUBr1NeCs0pqxEUbTThlsb50yjeaRCTSiTFMYyRC0oExiPM/wAqOs6zUweH+1IZKGoHGvUIA3z9KbRcHJZSk5qPlUpaXDYGp8qg7hzOvULgwco/rOtVYI1pZFXQMQSB5fKlE2QUnEnjnlUFavBaw3jSlZEpCpGI/hBiATGUkVI2F0RoTTKtEc4TlkwoJjDPhUjbs8jTa12qYEjPIDxPpTtu+zkg5VRIjWi624PGpG1SCMgKj03KCTkR8qf2N0BkONMILCynXWgNQcM/DMeMU5ddxRBzqHu7vq3usM
      //  mElMcgW/WVK8hQzR27amvBYqqeZbCkhQzBAIPI0s5b5Vn0gxlRctjnHjlUPdMmYirvcWw7qgr62BJp80TcK4u3y4+XyqPdtSCcKQD3VLbVQrVUzAA7h/Kq9f35aEqV3Ccz3UlYvSsa/Ct9KHFFwJPwjdxP8o86glJqT2ncFxZXGsc9BFRjtcV9vTvhYi7+xrpF9k2khCjDVzDK+AUT90r+Ls9yzXTlcTYiMwSCMwRqCNCOFdf9DdtC8sre5ylxsFUaBY7Kx4LChUbR0T4Ub+0DtPBZs24Ob7skcUNDEf8AOWq0mrYr4bS71Si2oSFCFZThkgSR2ssxrlV4/tDX+O/ZZ3NMA/mcWon0SioTZ93estNFhbbiCzigkBLaUrJ7XWFISZUZI4cgaeOkJfpXrazcWooQhalDMpSkqIjWQBI1A8RT+12M+okJbVIAOE5KgpWoQDmcm16cI1ImUXZ3TrTbgQnqylOFCScgFFSYxKk5kgAEgRAAp1bv3bbin4AKlArMhScw4lOIJUVYCOsg6
      //  GJByFVRFsi7fZL5/ZOHuQo7yOHFKh+U8KSNqZ0zG6M6tTouEp6o9WpOicOiRhIhOMhQAGPM8VZk14GJ9/3o1JBJ4Gd/8qrM6TdFaRZn8NFwQnJIz41OvoMRJiolxEGhrAT0jCg0oGqcFE07trWdaVIGyPRahSknDKhprNS1snjTli0zEajfzrN23wDEdJjxp0sEb09S/BAnQg+OIfzqUZuSZBg/0P1qBdUkgmTB+eXjlUrsdsrUokzAAMcT/tTJiUuiVQqn7DkCdP50i1aEU6Z2eZkk8hlA56a7vGmJDhD+EYuEExwkSfKTTMvTcoGRT1gxDUKClz8lIpW+7KCJzPZ/iy+U1Vbq+UyAMRxAhWLTMZj+uVZTGlGz+ji/uUJ1wlSB3IWpI9AKki/Vf2TdHqkEiMSQoxxV2j6k0/Q/R8JmaK32lVq8XnlUvcrJ35VXOlC8LDqgSDhIEZEE5Ag+NVnpEmtoq/SvbiWxhBCnOAOg0k6weRqvW+yA+jrVOkAILiiAFSQVFTaQVDCuAIBB
      //  nFikJo2Zs5lYh1aUQsSSsIVg6tcBIMgysNgmDAMmKRZtrcFIW44lLhwqAcbT1YGpWcJDycWhECBOuVcXJbpnocXGpXQnc2DQSOrdViWjGlKwBvUMEgmFDArMwM05iq+qrDa2FphAdeViwqKsCkFKTjASPdIcBQQTCsikic8md5a2/Uhxtag5ihTS1IWQNxCkpTO8nLeOBNTLIhVJrfn9nXa2O0ftiZLLoWkcEOjQfnQs/mrQztbI/s9XxRtFxqcnWFZfvIUlQ9MdJa6KyQvtgfLm17s6hJQgcglpE+s1AWW2nkAISrshJTEZFJ3HiMzUv0/OLad6T/3Cx/CcP0qulHKmS6Ep6y3bJtL0tNdWpJbXCkyUgBaF5JE/EClJ5g8jTqLoJLalJCQMJGJHugdduzIGKfHvqI2CzcvBTbTuFLYSpIUYBUVKhIOoISp9c55IPEVkhDyi3jdTK0KcTMk4AglRUAgz2QUxmTEcKZMm0SnXuBRSpWY1zkeFOWnid/KoNh4+8TNP7a6zmrTRNyS
      //  Dhpg+is3Lmki5Na6MwRSjOpixbyy303tmxUg2oJ4edCMY+tbQ7h41h0mQlLGhkqTGW/OfSaLfaaZjEJ/rfTHa94XlRMpTkOE7z9K110ZMtsj22cQzIAjKstiuFl8E6TB7jI/Q05bTBHj61i80Peg8P0pUxnJaNpbbCUobAAWojtawCRnzqS2dtAGULAxp1jRQOih51QFmCk8IOe4akelTNzeABC8WhjwO75+dNpJwSPSy4JDeAmMRkJyJJKQkTuzNVHpE0vCrGe0gkHTdukcKsD9yFJIkA5EH94GUnzAqDVajqlFSytxZKlDmfejeedLRs9GxOjhUq1YUvUtIP+UVKpIFVvZe2AtpChAlIy3AjIjwIIpyvagiZqyfRJok7hyqH07ulqKWU+6IKuJVqPAD51NjbABUSQMiEkiQFbiRB+RqK2pc26iVFCswrtkkqnF2YEwQE/6RxpbrrDYjvSr7F+zl1Sbkw2pIAMxCusbzncMOMGM4J30Nt2JT2wAoYirCpZBgqCYlZ3YDE7z2hG
      //  at+m3Wlag24lOIALzKWkmTiJ0yJSmCZIEgzNRN5tRgNNIShpRDYxBQIBdASDiUkBasusOSokiRlXNTOyUKXCbBKThSpSsIUPvFiVfgVuAyzynMwRlEftVNv1bamCcZxdYgqxYCCCIyzTBgH9znTx82odSSytNs4lJCjjSQUFWLCpX/ADAoYZCY1EEEZrvr2aQpWBZzOEthwIwhSiPe+LCW0q3SUnQk0mlcKmoVa/ZG8W9sWh3FakH87ax8yKj13NsluOpSoF1xQkqxoaUEYUgpV7wIVGOdDlBNSHR67ZG1bDqEhIF02nKTktxKRJPvHNRngoDdS0+h5Dp6zG0ryf8AuHD5qn60w2Tslb5UlCkApGLtmBEhMg8cSm0x++KsXtPtCnat3lkVpUPzNIPzmqyhChoSO4kbwc/EA+Apl4TZJp2ArGpDdwjrGiCRLiSDIEpVEEiVHIyBzkBY2jgQpwXhMJKyAXBiHbjfqerggjeNahQhUzJnvPf86zQlfEjxNaYSydlKwYy4iCAdSSCoI
      //  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" style="width: 178px; height: 179px; margin-left: 0px; margin-right: 0px; margin-top: 0px;">

      //*[@id="irc_cc"]/div[2]/div[1]/div[2]/div[2]/a/img
      //*[@id="irc_cc"]/div[3]/div[1]/div[2]/div[2]/a/img
      //*[@id="irc_cc"]/div[3]/div[1]/div[2]/div[2]/a/img
      //*[@id="irc_cc"]/div[3]/div[1]/div[2]/div[2]/a/img
      //*[@id="irc_cc"]/div[3]/div[1]/div[2]/div[2]/a/img
      //*[@id="irc_cc"]/div[2]/div[1]/div[2]/div[2]/a/img
      //*[@id="rg_s"]/div[1]
      //*[@id="rg_s"]/div[2]
      //*[@id="rg_s"]/div[1]/a

      SourceUrl = next ? NextEpisode.Result.Item2 : PrevEpisode.Result.Item2;
    }

    public class CommandHandler : ICommand
    {
      private Action _action;
      private bool _canExecute;
      public CommandHandler(Action action, bool canExecute)
      {
        _action = action;
        _canExecute = canExecute;
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
  }
}

