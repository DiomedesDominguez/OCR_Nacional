using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Text;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DNMOFT.OCR
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DrawImages();

        }
        private string INPUT_FILE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Demo", "receipt.jpg");
        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button).Tag.ToString();
            Mat image = new Mat();
            Mat image2 = new Mat();
            switch (tag)
            {
                case "0":
                    image = BitmapConverter.ToMat(ImageWpfToGDI(imgReceipt0.Source));
                    //Cv2.Erode(image, image2, new Mat(), iterations: 1);
                    //image2.SaveImage("modified.jpg");
                    break;
                case "1":
                    image = BitmapConverter.ToMat(ImageWpfToGDI(imgReceipt1.Source));
                    break;
                case "2":
                    image = BitmapConverter.ToMat(ImageWpfToGDI(imgReceipt2.Source));
                    break;
                default:
                    return;
            }

            RunTesseract(tag, image);

        }

        private void RunTesseract(string tag, Mat image)
        {
            using (var tesseract = OCRTesseract.Create(@"./tessdata", charWhitelist: @"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.,:/-\*"))
            {
                tesseract.Run(image,
                    out var outputText, out var componentRects, out var componentTexts, out var componentConfidences, ComponentLevels.TextLine);

                switch (tag)
                {
                    case "0":
                        Results0.Text = outputText;
                        break;
                    case "1":
                        Results1.Text = outputText;
                        break;
                    case "2":
                        Results2.Text = outputText;
                        break;
                    default:
                        break;
                }
            }
        }

        private Bitmap ImageWpfToGDI(System.Windows.Media.ImageSource image)
        {
            MemoryStream ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image as System.Windows.Media.Imaging.BitmapSource));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms) as Bitmap;
        }
        private void DrawImages()
        {
            Mat large = new Mat(INPUT_FILE);
            Mat rgb = new Mat(), small = new Mat(), grad = new Mat(), bw = new Mat(), connected = new Mat();

            // downsample and use it for processing
            Cv2.PyrDown(large, rgb);

            Cv2.CvtColor(rgb, small, ColorConversionCodes.BGR2GRAY);

            // morphological gradient
            var morphKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
            Cv2.MorphologyEx(small, grad, MorphTypes.Gradient, morphKernel);

            // binarize
            Cv2.Threshold(grad, bw, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            imgReceipt0.Source = WriteableBitmapConverter.ToWriteableBitmap(large);
            imgReceipt1.Source = WriteableBitmapConverter.ToWriteableBitmap(grad);
            imgReceipt2.Source = WriteableBitmapConverter.ToWriteableBitmap(bw);
        }

    }
}
