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
        List<string> labels = new List<string>();
        List<string> NamePersons = new List<string>();
        int ContTrain, NumLabels, t;
        string name, names = null;

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
            var connectionstring = ConfigurationManager.ConnectionStrings["Test"].ConnectionString;
            using(SqlConnection connection = new SqlConnection(connectionstring))
            {
                connection.Open();
                string query = "SELECT * FROM Attendance.dbo.TrainingData";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            labels.Add(reader.GetString(0));
                            byte[] blob = null;
                            blob = (byte[])reader.GetValue(2);
                            var image = ByteToImage(blob);
                            Image<Gray, Byte> newImage = new Image<Gray, Byte>(image);
                            trainingImages.Add(newImage);
                        }
                    }
                }

            }
            Image<Bgr, Byte> currentFrame = capture.QueryFrame().ToImage<Bgr,Byte>();
            if (currentFrame != null)
            {
                Image<Gray, Byte> grayFrame = currentFrame.Convert<Gray, Byte>();

                var detectedFaces = Frontface_Cascade.DetectMultiScale(grayFrame);
                var detectedFaces2 = EyeGlass_Cascade.DetectMultiScale(grayFrame);

                try
                {
                    //Load of previus trainned faces and labels for each image
                    string Labelsinfo = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "/TrainedFaces/TrainedLabels.txt");
                    logger.Info(Labelsinfo);
                    string[] Labels = Labelsinfo.Split('%');
                    NumLabels = Convert.ToInt16(Labels[0]);
                    ContTrain = NumLabels;
                    string LoadFaces;

                    for (int tf = 1; tf < NumLabels + 1; tf++)
                    {
                        LoadFaces = "face" + tf + ".bmp";
                        trainingImages.Add(new Image<Gray, Byte>(AppDomain.CurrentDomain.BaseDirectory + "/TrainedFaces/" + LoadFaces));
                        labels.Add(Labels[tf]);
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(e.ToString());
                    logger.Warn(ex);
                    MessageBox.Show("Nothing in binary database, please add at least a face", "Trained faces load");
                }
                try
                {
                    //Trained face counter
                    ContTrain = ContTrain + 1;

                    //Get a gray frame from capture device
                    gray = capture.QueryFrame().ToImage<Gray, Byte>();

                    //Face Detector
                    var facesDetected = Frontface_Cascade.DetectMultiScale(gray, 1.2, 19, System.Drawing.Size.Empty, System.Drawing.Size.Empty);
                    //Action for each element detected
                    foreach (var f in facesDetected)
                    {
                        TrainedFace = currentFrame.Copy(f).Convert<Gray,Byte>();
                        break;
                    }

                    //resize face detected image for force to compare the same size with the
                    //test image with cubic interpolation type method
                    
                    trainingImages.Add(TrainedFace);
                    labels.Add(textbox1.Text);

                    //Show face added in gray scale
                    

                    //Write the number of triained faces in a file text for further load
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                    //Write the labels of triained faces in a file text for further load
                    for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
                    {
                        trainingImages.ToArray()[i - 1].Save(AppDomain.CurrentDomain.BaseDirectory + "/TrainedFaces/face" + i + ".bmp");
                        File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                    }

                    MessageBox.Show(textbox1.Text + "´s face detected and added :)", "Training OK");
                }
                catch (Exception ex)
                {
                    logger.Warn(ex);
                    MessageBox.Show("Enable the face detection first", "Training Fail");
                }
                foreach (var face in detectedFaces)
                {
                    t = t + 1;
                    result = grayFrame.Copy(face);
                    //draw the face detected in the 0th (gray) channel with blue color
                    currentFrame.Draw(face, new Bgr(0, double.MaxValue, 0), 3);

                    if (trainingImages.ToArray().Length != 0)
                    {
                        //TermCriteria for face recognition with numbers of trained images like maxIteration
                        MCvTermCriteria termCrit = new MCvTermCriteria(ContTrain, 0.001);

                        //Eigen face recognizer
                        EigenFaceRecognizer recognizer = new EigenFaceRecognizer(1,5000);
                        //recognizer.Train(trainingImages.ToArray(), labels.ToArray());
                        //name = recognizer.Predict(result).ToString();

                        //Draw the label for each face detected and recognized
                        textbox1.Text = name;
                    }
                        
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
