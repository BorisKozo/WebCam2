using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using CatenaLogic.Windows.Presentation.WebcamPlayer;

namespace WebCam2
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    enum HotSpotMode { None, Start, End };

    private DateTime _lastTime = DateTime.Now;
    private Queue<BitmapSource> _images = new Queue<BitmapSource>();
    private int _maxBuffer = 30;
    private string _dataPath;
    private CaptureObject _capturer = null;
    private int _fps;
    DispatcherTimer _dispatcherTimer = new DispatcherTimer();



    private HotSpotMode _hotSpotMode = HotSpotMode.None;
    private Point _hotSpotStartPoint;
    private List<HotSpot> _hotSpots = new List<HotSpot>();
    private System.Windows.Shapes.Rectangle _tempRectangle = null;

    private CapDevice _capDevice;

    private void CaptureVideo()
    {
      if (_capturer != null)
      {
        return;
      }

      _capturer = new CaptureObject(_dataPath);
      _capturer.Start(_maxBuffer * 2, _images, _fps);
      _capturer.CaptureFinished += _capturer_CaptureFinished;
      StatusBarItem1.Text = "Capturing";
    }

    void _capturer_CaptureFinished(object sender, EventArgs e)
    {
      _capturer.SaveAsVideo();
      _capturer = null;
      _images.Clear();
      StatusBarItem1.Text = "Done";
    }

    public MainWindow()
    {
      InitializeComponent();
      MainPlayer.Rotation = 180;
      _capDevice = new CapDevice(CapDevice.DeviceMonikers[0].MonikerString);
      MainPlayer.Device = _capDevice;

      _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(150);
      _dispatcherTimer.Tick += dispatcherTimer_Tick;

      string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      _dataPath = Path.Combine(directoryName, "Data");

      if (!Directory.Exists(_dataPath))
      {
        Directory.CreateDirectory(_dataPath);
      }

      var hotSpotsFileName = Path.Combine(directoryName, "hotspots.txt");
      if (File.Exists(hotSpotsFileName))
      {
        using (StreamReader reader = new StreamReader(hotSpotsFileName))
        {
          while (!reader.EndOfStream)
          {
            string line = reader.ReadLine();
            string[] data = line.Split(',');
            Rect rect = new Rect(Convert.ToDouble(data[0]), Convert.ToDouble(data[1]), Convert.ToDouble(data[2]), Convert.ToDouble(data[3]));
            HotSpot hotSpot = new HotSpot(rect);
            AddHotSpot(hotSpot);

          }
        }
      }


    }

    void dispatcherTimer_Tick(object sender, EventArgs e)
    {
      DateTime time = DateTime.Now;
      TimeSpan span = time - _lastTime;
      _lastTime = time;
      _fps = Convert.ToInt32(Math.Round(1000.0 / Math.Max(span.TotalMilliseconds, 1)));
      FpsLabel.Text = "FPS: " + _fps.ToString();
      BitmapSource bitmap = MainPlayer.CurrentBitmap.Clone();
      BitmapSource prevImage = null;
      if (_capturer != null)
      {
        _capturer.Capture(bitmap);
      }
      else
      {
        if (_images.Count > 0)
          prevImage = _images.Last();
        _images.Enqueue(bitmap);
        if (_images.Count > _maxBuffer)
          _images.Dequeue();

        if (_images.Count == _maxBuffer && _hotSpots.Count > 0)
        {
          bool checkChanges = CheckChanges(bitmap, prevImage, _hotSpots, Convert.ToInt32(SensitivitySlider.Value));
          if (checkChanges)
          {
            CaptureVideo();
          }
        }
      }


    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
      _dispatcherTimer.Start();
    }

    private bool CheckChanges(BitmapSource image, BitmapSource prevImage, List<HotSpot> hotSpots, int sensitivity)
    {

      System.Drawing.Bitmap innerImage = CaptureObject.BitmapFromSource(image);
      System.Drawing.Bitmap innerPrevImage = CaptureObject.BitmapFromSource(prevImage);
      foreach (HotSpot hotSpot in hotSpots)
      {
        float sum = 0;
        float result = 0;
        float leftPixel;
        float rightPixel;
        int left = Convert.ToInt32(hotSpot.Bound.Left);
        int right = Convert.ToInt32(hotSpot.Bound.Right);
        int top = Convert.ToInt32(hotSpot.Bound.Top);
        int bottom = Convert.ToInt32(hotSpot.Bound.Bottom);
        for (int i = left; i < right; i++)
        {
          for (int j = top; j < bottom; j++)
          {
            leftPixel = RgbToGray(innerImage.GetPixel(i, j));
            rightPixel = RgbToGray(innerPrevImage.GetPixel(i, j));
            result += Math.Abs(leftPixel - rightPixel);
            sum += 1;
          }
        }

        result = result / sum;
        Console.WriteLine(result);
        if (result > sensitivity)
        {
          return true;
        }
      }

      return false;

    }

    private float RgbToGray(System.Drawing.Color color)
    {
      return 0.299f * Convert.ToSingle(color.R) + 0.587f * Convert.ToSingle(color.G) + 0.114f * Convert.ToSingle(color.B);
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
      if (_capturer != null)
      {
        return;
      }
      CaptureVideo();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
      _hotSpotMode = HotSpotMode.Start;
    }

    private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (_hotSpotMode == HotSpotMode.Start)
      {
        _hotSpotStartPoint = e.GetPosition(MainCanvas);
        _hotSpotMode = HotSpotMode.End;
        _tempRectangle = new System.Windows.Shapes.Rectangle();
        _tempRectangle.Stroke = Brushes.Red;
        MainCanvas.Children.Add(_tempRectangle);
        return;
      }

      if (_hotSpotMode == HotSpotMode.End)
      {
        Point point = e.GetPosition(MainCanvas);
        int x = Convert.ToInt32(Math.Truncate(Math.Min(_hotSpotStartPoint.X, point.X)));
        int y = Convert.ToInt32(Math.Truncate(Math.Min(_hotSpotStartPoint.Y, point.Y)));
        int width = Convert.ToInt32(Math.Truncate(Math.Abs(_hotSpotStartPoint.X - point.X)));
        int height = Convert.ToInt32(Math.Truncate(Math.Abs(_hotSpotStartPoint.Y - point.Y)));
        HotSpot hotSpot = new HotSpot(new Rect(x, y, width, height));
        MainCanvas.Children.Remove(_tempRectangle);
        _tempRectangle = null;
        AddHotSpot(hotSpot);
        _hotSpotMode = HotSpotMode.None;
        return;
      }
    }

    private void AddHotSpot(HotSpot hotSpot)
    {
      _hotSpots.Add(hotSpot);
      HotSpotsList.Items.Add(hotSpot.ToString());
      System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
      rect.Stroke = Brushes.Blue;
      rect.Width = hotSpot.Bound.Width;
      rect.Height = hotSpot.Bound.Height;
      Canvas.SetLeft(rect, hotSpot.Bound.X);
      Canvas.SetTop(rect, hotSpot.Bound.Y);
      MainCanvas.Children.Add(rect);
    }

    private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
    {
      if (_hotSpotMode == HotSpotMode.End && _tempRectangle != null)
      {
        Point point = e.GetPosition(MainCanvas);
        int x = Convert.ToInt32(Math.Truncate(Math.Min(_hotSpotStartPoint.X, point.X)));
        int y = Convert.ToInt32(Math.Truncate(Math.Min(_hotSpotStartPoint.Y, point.Y)));
        int width = Convert.ToInt32(Math.Truncate(Math.Abs(_hotSpotStartPoint.X - point.X)));
        int height = Convert.ToInt32(Math.Truncate(Math.Abs(_hotSpotStartPoint.Y - point.Y)));
        _tempRectangle.Width = width;
        _tempRectangle.Height = height;
        Canvas.SetLeft(_tempRectangle, x);
        Canvas.SetTop(_tempRectangle, y);
      }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
      if (HotSpotsList.SelectedIndex >= 0)
      {
        _hotSpots.RemoveAt(HotSpotsList.SelectedIndex);
        MainCanvas.Children.RemoveAt(HotSpotsList.SelectedIndex);
        HotSpotsList.Items.RemoveAt(HotSpotsList.SelectedIndex);
      }
    }

  }
}
