using System;
using System.Collections.Generic;
using System.IO;
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
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Data.SqlClient;
using System.Drawing;
using System.Configuration;

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
        Image<Gray,Byte> result, TrainedFace = null;
        Image<Gray, Byte> gray = null;
        List<Image<Gray,Byte>> trainingImages = new List<Image<Gray,Byte>>();
        List<int> labels = new List<int>();
        List<string> NamePersons = new List<string>();
        int ContTrain, NumLabels, t;
        string name, names = null;
        EigenFaceRecognizer recognizer;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            capture = new VideoCapture();
            Frontface_Cascade = new CascadeClassifier(@"haarcascades/haarcascade_frontalface_default.xml");
            EyeGlass_Cascade = new CascadeClassifier(@"haarcascades/haarcascade_eye_tree_eyeglasses.xml");
            var connectionstring = ConfigurationManager.ConnectionStrings["Test"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                connection.Open();
                string query = "SELECT * FROM Attendance.dbo.TrainingData";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!NamePersons.Any() || NamePersons[ContTrain] == reader.GetString(0))
                            {
                                labels.Add(NumLabels);
                            }
                            else
                            {
                                NumLabels++;
                                labels.Add(NumLabels);
                            }
                            NamePersons.Add(reader.GetString(0));
                            byte[] blob = null;
                            blob = (byte[])reader.GetValue(2);
                            var image = ByteToImage(blob);
                            Image<Gray, Byte> newImage = new Image<Gray, Byte>(image);
                            trainingImages.Add(newImage);
                        }
                    }
                }
            }
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
                recognizer = new EigenFaceRecognizer(1, 5000);

                Mat[] faceImages = new Mat[trainingImages.Count];
                int[] faceLabels = new int[labels.Count];

                for (int i = 0; i < trainingImages.Count; i++)
                {
                    var face = Frontface_Cascade.DetectMultiScale(trainingImages[i]);
                    foreach (var Tface in detectedFaces)
                    {
                        trainingImages[i].ROI = Tface;
                    }
                    gray = trainingImages[i].Clone().Resize(60,60,0);
                     Mat x = gray.Mat;
                    faceImages[i] = x;
                }

                for (int i = 0; i < labels.Count; i++)
                {
                    faceLabels = labels.ToArray();
                }
                recognizer.Train(faceImages, faceLabels);
                logger.Info("Training Face Recognizer");
                foreach (var face in detectedFaces)
                {
                    grayFrame.ROI = face;
                    currentFrame.Draw(face, new Bgr(0, double.MaxValue, 0), 3);                        
                    logger.Info("Drawing Rectangle Outline of Face");
                    textbox1.Text = NamePersons[recognizer.Predict(grayFrame.Resize(60,60,0)).Label];
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

        public static Bitmap ByteToImage(byte[] blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = blob;
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;

        }
    }

}
