using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Text;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Tesseract;

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
            DrawLettersBoxes();

        }
        private string INPUT_FILE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Demo", "receipt.jpg");
        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button).Tag.ToString();
            //UseTesseract3(tag);
            Mat image = new Mat();
            switch (tag)
            {
                case "0":
                    image = BitmapConverter.ToMat(ImageWpfToGDI(imgReceipt0.Source));
                    break;
                case "1":
                    image = BitmapConverter.ToMat(ImageWpfToGDI(imgReceipt1.Source));
                    break;
                case "2":
                    image = BitmapConverter.ToMat(ImageWpfToGDI(imgReceipt2.Source));
                    break;
                case "3":
                    image = BitmapConverter.ToMat(ImageWpfToGDI(imgReceipt3.Source));
                    break;
                default:
                    break;
            }
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
                    case "3":
                        Results3.Text = outputText;
                        break;
                    default:
                        break;
                }
            }
        }

        private void UseTesseract3(string tag)
        {
            switch (tag)
            {
                case "0":
                    Results0.Text = GetText(ImageWpfToGDI(imgReceipt0.Source));
                    break;
                case "1":
                    Results1.Text = GetText(ImageWpfToGDI(imgReceipt1.Source));
                    break;
                case "2":
                    Results2.Text = GetText(ImageWpfToGDI(imgReceipt2.Source));
                    break;
                case "3":
                    Results3.Text = GetText(ImageWpfToGDI(imgReceipt3.Source));
                    break;
                default:
                    break;
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
        public string GetText(Bitmap imgsource)
        {
            var ocrtext = string.Empty;
            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            {
                using (var img = PixConverter.ToPix(imgsource))
                {
                    using (var page = engine.Process(img))
                    {
                        ocrtext = page.GetText();
                    }
                }
            }

            return ocrtext;
        }

        private void DrawLettersBoxes()
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

            // connect horizontally oriented regions
            morphKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 1));
            Cv2.MorphologyEx(bw, connected, MorphTypes.Close, morphKernel);

            // find contours
            var mask = new Mat(bw.Size(), MatType.CV_8UC1);

            Cv2.FindContours(connected, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

            var hierarchyLength = hierarchy.Length;
            hierarchy = null;

            // filter contours
            for (int idx = 0; idx < hierarchyLength; idx++)
            {
                OpenCvSharp.Rect rect = Cv2.BoundingRect(contours[idx]);
                var maskROI = new Mat(mask, rect);
                maskROI.SetTo(new Scalar(0, 0, 0));

                // fill the contour
                Cv2.DrawContours(mask, contours, idx, Scalar.White, -1);

                // ratio of non-zero pixels in the filled region
                double r = (double)Cv2.CountNonZero(maskROI) / (rect.Width * rect.Height);
                if (r > .45 /* assume at least 45% of the area is filled if it contains text */
                     &&
                (rect.Height > 8 && rect.Width > 8) /* constraints on region size */
                /* these two conditions alone are not very robust. better to use something 
                like the number of significant peaks in a horizontal projection as a third condition */
                )
                {
                    Cv2.Rectangle(rgb, rect, new Scalar(0, 255, 0), 2);
                }
            }
            imgReceipt0.Source = WriteableBitmapConverter.ToWriteableBitmap(large);
            imgReceipt1.Source = WriteableBitmapConverter.ToWriteableBitmap(grad);
            imgReceipt2.Source = WriteableBitmapConverter.ToWriteableBitmap(bw);
            imgReceipt3.Source = WriteableBitmapConverter.ToWriteableBitmap(rgb);
        }

    }
}
