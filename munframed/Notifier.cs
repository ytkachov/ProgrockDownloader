using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace munframed
{
  class Notifier : INotifyPropertyChanged
  {
    #region Events

    public event PropertyChangedEventHandler PropertyChanged;
    protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string caller_name = null)
    {
      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(caller_name));
    }

    #endregion
  }
}
