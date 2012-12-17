using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using AviFile;

namespace WebCam2
{
  class CaptureObject
  {
    private string _targetPath;
    private List<BitmapSource> _images;
    private int _counter = 0;
    private int _fps = 15;

    public event EventHandler CaptureFinished;

    public CaptureObject(string path)
    {
      _targetPath = Path.Combine(path, DateTime.Now.ToString("yyyyMMdd_HHmmss_F"));
      Directory.CreateDirectory(_targetPath);
    }


    public void Start(int futureImagesCount, IEnumerable<BitmapSource> previousImages, int fps)
    {
      _images = new List<BitmapSource>();
      foreach (BitmapSource bitmap in previousImages)
      {
        _images.Add(bitmap);
      }

      _counter = futureImagesCount;
      _fps = fps;
    }

    public void Capture(BitmapSource image)
    {
      if (Counter > 0)
      {
        _images.Add(image);
        _counter--;
        if (Counter == 0 && CaptureFinished != null)
        {
          CaptureFinished(this, new EventArgs());
        }
      }
    }

    public void Save()
    {
      int counter = 0;
      
      foreach (BitmapSource image in _images)
      {
        using (FileStream stream = new FileStream(Path.Combine(_targetPath, "image" + counter.ToString() + ".png"), FileMode.Create))
        {
          PngBitmapEncoder encoder = new PngBitmapEncoder();
          encoder.Interlace = PngInterlaceOption.On;
          encoder.Frames.Add(BitmapFrame.Create(image));
          encoder.Save(stream);
          counter++;
        }
      }
      ClearImages();
    }

    public static System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
    {
      System.Drawing.Bitmap bitmap;
      using (MemoryStream outStream = new MemoryStream())
      {
        BitmapEncoder enc = new BmpBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bitmapsource));
        enc.Save(outStream);
        bitmap = new System.Drawing.Bitmap(outStream);
      }
      return bitmap;
    }

    public void SaveAsVideo()
    {

      BitmapSource bmp = _images[0];
      AviManager aviManager = new AviManager(Path.Combine(_targetPath, "video.avi"), false);
      VideoStream aviStream = aviManager.AddVideoStream(false, _fps,BitmapFromSource(bmp));
      for (int n = 1; n < _images.Count; n++)
      {
        aviStream.AddFrame(BitmapFromSource(_images[n]));
      }

      ClearImages();
      aviManager.Close();
    }

    private void ClearImages()
    {
      _images.Clear();
    }


    public int Counter
    {
      get { return _counter; }
    }
  }
}
