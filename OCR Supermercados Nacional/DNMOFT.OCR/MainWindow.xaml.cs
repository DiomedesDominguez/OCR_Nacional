using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.IO;
using System.Windows;

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

        }
        private string INPUT_FILE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Demo", "receipt.jpg");
        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            DrawLettersBoxes();
        }

        private void DrawLettersBoxes()
        {
            Mat large = new Mat(INPUT_FILE);
            Mat rgb = new Mat(), small = new Mat(), grad = new Mat(), bw = new Mat(), connected = new Mat();

            // downsample and use it for processing
            Cv2.PyrDown(large, rgb);
            large.Dispose();

            Cv2.CvtColor(rgb, small, ColorConversionCodes.BGR2GRAY);

            // morphological gradient
            var morphKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
            Cv2.MorphologyEx(small, grad, MorphTypes.Gradient, morphKernel);
            small.Dispose();

            // binarize
            Cv2.Threshold(grad, bw, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // connect horizontally oriented regions
            morphKernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(9, 1));
            Cv2.MorphologyEx(bw, connected, MorphTypes.Close, morphKernel);

            // find contours
            var mask = new Mat(bw.Size(), MatType.CV_8UC1);

            Cv2.FindContours(connected, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple, new OpenCvSharp.Point(0, 0));

            // filter contours
            var idx = 0;
            foreach (var hierarchyItem in hierarchy)
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
                idx++;
            }
            imgReceipt.Source = WriteableBitmapConverter.ToWriteableBitmap(rgb);
        }

    }
}
