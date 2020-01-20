using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;
using NLog;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Drawing;

namespace Attendance
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private VideoCapture capture;
        private CascadeClassifier Frontface_Cascade;
        private CascadeClassifier EyeGlass_Cascade;
        DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            capture = new VideoCapture();
            Frontface_Cascade = new CascadeClassifier(@"haarcascades/haarcascade_frontalface_default.xml");
            EyeGlass_Cascade = new CascadeClassifier(@"haarcascades/haarcascade_eye_tree_eyeglasses.xml");
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Image<Bgr, Byte> currentFrame = capture.QueryFrame().ToImage<Bgr,Byte>();
            if (currentFrame != null)
            {
                Image<Gray, Byte> grayFrame = currentFrame.Convert<Gray, Byte>();

                var detectedFaces = Frontface_Cascade.DetectMultiScale(grayFrame);
                var detectedFaces2 = EyeGlass_Cascade.DetectMultiScale(grayFrame);

                foreach (var face in detectedFaces)
                {
                    currentFrame.Draw(face, new Bgr(0, double.MaxValue, 0), 3);
                    logger.Info("Drawing Rectangle Outline of Face");
                }
                foreach (var face in detectedFaces2)
                {
                    currentFrame.Draw(face, new Bgr(0, double.MaxValue, 0), 3);
                    logger.Info("Drawing Rectangle Outline of Eyeglasses");
                }

                image1.Source = ToBitmapSource(currentFrame);
            }
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(Image<Bgr, Byte> image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap  

                BitmapSource bs = System.Windows.Interop
                  .Imaging.CreateBitmapSourceFromHBitmap(
                  ptr,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap  
                return bs;
            }
        }

    }
}
