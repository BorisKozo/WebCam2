using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace WebCam2
{
  public class HotSpot
  {
    private Rect _bound;

    public HotSpot(Rect bound)
    {
      _bound = bound;
    }

    public override string ToString()
    {
      return string.Format("({0},{1},{2},{3})", Bound.Left, Bound.Top, Bound.Width, Bound.Height);
    }

    public Rect Bound
    {
      get { return _bound; }
    }




  }
}
