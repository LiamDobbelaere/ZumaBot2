using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = OpenCvSharp.Point;

namespace ZumaBot2 {
    public partial class MainForm : Form {
        private Point windowLocation;
        private bool foundGameWindow = false;

        private Bitmap gameWindow = new Bitmap(640, 480, PixelFormat.Format32bppRgb);
        private Bitmap froggy = new Bitmap(128, 128, PixelFormat.Format32bppRgb);
        private Thread captureThread;

        private const int defaultFrameDelay = 16;

        private List<Color> colorMatchers = new List<Color>();
        private List<Color> fixedColors = new List<Color>();


        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Red, "ct_red.png");
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Green, "ct_green.png");
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Blue, "ct_blue.png");
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Yellow, "ct_yellow.png");
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Violet, "ct_violet.png");

            FindGameWindow();

            captureThread = new Thread(() => CaptureThread());
            captureThread.Start();
        }

        private string GetAssetPath(string filename) {
            return Path.Combine(Directory.GetCurrentDirectory(), "images", filename);
        }

        private void FindGameWindow() {
            Bitmap screen = new Bitmap(1920, 1080, PixelFormat.Format32bppRgb);

            using var mTemplate = Cv2.ImRead(GetAssetPath("game_window_target_mt.png"));

            string[] textDots = new string[] {
                " |",
                " /",
                " -",
                " \\",
                " |",
                " /",
                " -",
                " \\"
            };
            int textDotsIndex = 0;

            double minVal;
            double maxVal;
            Point minLoc;
            Point maxLoc;

            using var mResult = new Mat();
            using Graphics g = Graphics.FromImage(screen);

            while (!foundGameWindow) {
                g.CopyFromScreen(0, 0, 0, 0, screen.Size);

                using var mScreen = screen.ToMat();

                Cv2.MatchTemplate(mScreen, mTemplate, mResult, TemplateMatchModes.CCoeffNormed);

                Cv2.MinMaxLoc(mResult, out minVal, out maxVal, out minLoc, out maxLoc);
                Cv2.PutText(mResult, "Finding game window" + textDots[textDotsIndex], new Point(64, 64), HersheyFonts.HersheyPlain, 2f, Scalar.White, 1);

                Cv2.ImShow("out", mResult);
                Cv2.WaitKey(defaultFrameDelay);

                if (maxVal > 0.99d) {
                    windowLocation = maxLoc;
                    foundGameWindow = true;
                    Cv2.PutText(mResult, "Succeeded! " + maxLoc.X + ", " + maxLoc.Y, new Point(64, 128), HersheyFonts.HersheyPlain, 2f, Scalar.White, 2);
                    Cv2.ImShow("out", mResult);
                    Cv2.WaitKey(1000);
                }

                if (++textDotsIndex >= textDots.Length) {
                    textDotsIndex = 0;
                }
            }
        }

        private void ColorDetectionLoadInto(List<Color> colorMatchers, List<Color> fixedColors, Color target, string assetName) {
            string fullPath = GetAssetPath(assetName);

            Bitmap bmp = new Bitmap(fullPath);

            for (int y = 0; y < bmp.Height; y++) {
                for (int x = 0; x < bmp.Width; x++) {
                    Color c = bmp.GetPixel(x, y);

                    colorMatchers.Add(c);
                    fixedColors.Add(target);
                }
            }
        }

        private void CDTester(Mat original) {
            using var mFrogTemplate = new Mat(GetAssetPath("frog_test.png"));

            using ORB orb = ORB.Create();
            KeyPoint[] keypointsOriginal;
            using var descriptorsOriginal = new Mat();

            KeyPoint[] keypointsTemplate;
            using var descriptorsTemplate = new Mat();

            using var mOriginalGray = original.CvtColor(ColorConversionCodes.BGR2GRAY);
            using var mFrogTemplateGray = mFrogTemplate.CvtColor(ColorConversionCodes.BGR2GRAY);

            orb.DetectAndCompute(mOriginalGray, null, out keypointsOriginal, descriptorsOriginal);
            orb.DetectAndCompute(mFrogTemplateGray, null, out keypointsTemplate, descriptorsTemplate);

            using var bfm = BFMatcher.Create("BruteForce-Hamming");
            var matches = bfm.Match(descriptorsTemplate, descriptorsOriginal);

            var orderedMatches = matches.OrderBy((k) => k.Distance).Take(10).ToArray();

            Point com = CenterOfMass(keypointsOriginal, orderedMatches);

            using Graphics g = Graphics.FromImage(froggy);
            g.DrawImage(gameWindow, -(com.X - froggy.Width / 2), -(com.Y - froggy.Height / 2));

            using var coloredFroggy = froggy.ToMat();
            using var croppedFroggy = froggy
                .ToMat()
                .CvtColor(ColorConversionCodes.RGB2HSV)
                .ExtractChannel(1)
                .Threshold(100, 255, ThresholdTypes.Binary);

            Cv2.BitwiseAnd(coloredFroggy, croppedFroggy.CvtColor(ColorConversionCodes.GRAY2BGR), coloredFroggy);

            Color mostPrevalent = FindMostPrevalentColor(coloredFroggy.ToBitmap());

            Cv2.Rectangle(coloredFroggy, new Rect(0, 0, 32, 32), new Scalar(mostPrevalent.B, mostPrevalent.G, mostPrevalent.R), -1);

            Cv2.ImShow("froggy", coloredFroggy);

            /*
            ConnectedComponents frogCc = Cv2.ConnectedComponentsEx(croppedFroggy);
            using var mCroppedFroggyLargest = new Mat();
            if (frogCc.Blobs.Count > 0) {
                frogCc.FilterByBlob(croppedFroggy, mCroppedFroggyLargest, frogCc.GetLargestBlob());

                CircleSegment[] circles = Cv2.HoughCircles(mCroppedFroggyLargest, HoughModes.Gradient, 16, 1, 50, 50, 1, 50);
                foreach (CircleSegment segment in circles) {
                    Cv2.Circle(
                         coloredFroggy,
                         (int)segment.Center.X,
                         (int)segment.Center.Y,
                         (int)segment.Radius,
                         Scalar.Blue,
                         2
                     );
                }

                //Cv2.ImShow("frog", coloredFroggy);
            }*/

            /*using var mFinalOut = new Mat();
            Cv2.DrawMatches(mFrogTemplateGray, keypointsTemplate, mOriginalGray, keypointsOriginal, orderedMatches, mFinalOut);
            Cv2.PutText(mFinalOut, orderedMatches.Length.ToString(), new Point(64, 64), HersheyFonts.HersheySimplex, 2f, Scalar.White, 2);
            Cv2.PutText(mFinalOut, keypointsOriginal.Length.ToString() + ", " + keypointsTemplate.Length.ToString(), new Point(64, 128), HersheyFonts.HersheySimplex, 2f, Scalar.White, 2);
            Cv2.Circle(mFinalOut, CenterOfMass(keypointsOriginal, orderedMatches) + new Point(mFrogTemplateGray.Width, 0), 16, Scalar.White, 2);

            Cv2.ImShow("out2", mFinalOut);*/

            /*
            Dictionary<string, int> selParams = new Dictionary<string, int>();
            selParams["p1"] = 100;
            selParams["p2"] = 100;
            selParams["minRadius"] = 0;
            selParams["maxRadius"] = 32;
            selParams["minDist"] = 1;
            selParams["dp"] = 1;

            CircleSegment[] circles = Cv2.HoughCircles(mCroppedFroggyLargest, HoughModes.Gradient, 16, 20, 30, 80, 4, 25);
            foreach (CircleSegment segment in circles) {
                Cv2.Circle(
                     mCroppedFroggyLargest,
                     (int)segment.Center.X,
                     (int)segment.Center.Y,
                     (int)segment.Radius,
                     Scalar.Blue,
                     2
                 );
            }

            Cv2.ImShow("Froggy", mCroppedFroggyLargest);*/

            Cv2.WaitKey(defaultFrameDelay);
        }

        private void CaptureThread() {
            using Graphics g = Graphics.FromImage(gameWindow);

            while (true) {
                g.CopyFromScreen(windowLocation.X, windowLocation.Y, 0, 0, gameWindow.Size);

                // Convert the game capture, but only pay attention to the saturation channel (balls are more saturated than the backgrounds)
                using var mGameOriginal = gameWindow.ToMat();
                using var mGameColor = gameWindow.ToMat();
                using var mGame = gameWindow
                    .ToMat()
                    .CvtColor(ColorConversionCodes.RGB2HSV)
                    .ExtractChannel(1)
                    .Threshold(128, 255, ThresholdTypes.Binary);

                CDTester(mGameColor);

                // Get rid of small noise speckles
                Cv2.FilterSpeckles(mGame, 0, 16, 8);

                // Fill holes
                using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(8, 8));
                Cv2.MorphologyEx(mGame, mGame, MorphTypes.Close, kernel);

                // Get largest connected component (the ball chain)
                ConnectedComponents cc = Cv2.ConnectedComponentsEx(mGame, PixelConnectivity.Connectivity4);
                using Mat mGameLargestBlob = new Mat();
                if (cc.Blobs.Count == 0) {
                    continue;
                }

                cc.FilterByBlob(mGame, mGameLargestBlob, cc.GetLargestBlob());

                // Create a composite of the previous step (binary largest blob image + original color)
                Cv2.BitwiseAnd(mGameColor, mGameLargestBlob.CvtColor(ColorConversionCodes.GRAY2BGR), mGameColor);

                // Color them ballz
                CircleSegment[] circles = Cv2.HoughCircles(mGameLargestBlob, HoughModes.Gradient, 5, 20, 30, 35, 0, 20);
                foreach (CircleSegment segment in circles) {
                    int col = mGameLargestBlob.At<int>((int)segment.Center.Y, (int)segment.Center.X);

                    if (col < 0) {
                        Vec3b ccol = mGameOriginal.At<Vec3b>((int)segment.Center.Y, (int)segment.Center.X);
                        Color ccolAsColor = Color.FromArgb(ccol[2], ccol[1], ccol[0]);
                        Color usedColor = Color.Orange;
                        int usedColorIndex = -1;
                        float lowestDistance = float.MaxValue;
                        for (int k = 0; k < colorMatchers.Count; k++) {
                            Color cs = colorMatchers[k];

                            float colorDist = MathF.Sqrt(
                                MathF.Pow(ccolAsColor.R - cs.R, 2) +
                                MathF.Pow(ccolAsColor.G - cs.G, 2) +
                                MathF.Pow(ccolAsColor.B - cs.B, 2)
                            );

                            if (colorDist < lowestDistance) {
                                usedColor = cs;
                                usedColorIndex = k;
                                lowestDistance = colorDist;
                            }
                        }

                        if (usedColorIndex >= 0) {
                            Color fixedColor = fixedColors[usedColorIndex];

                            Cv2.Circle(
                                mGameColor,
                                (int)segment.Center.X,
                                (int)segment.Center.Y,
                                (int)segment.Radius,
                                new Scalar(fixedColor.B, fixedColor.G, fixedColor.R),
                                2
                            );
                        }
                    }
                }


                this.Invoke(() => {
                    Cv2.ImShow("out", mGameColor);
                    Cv2.WaitKey(defaultFrameDelay);
                });
            }
        }

        private Point CenterOfMass(KeyPoint[] keypoints, DMatch[] arr) {
            Point center = new Point();

            foreach (DMatch match in arr) {
                center.X += keypoints[match.TrainIdx].Pt.ToPoint().X;
                center.Y += keypoints[match.TrainIdx].Pt.ToPoint().Y;
            }

            center.X = (int)MathF.Round(center.X / (float)arr.Length);
            center.Y = (int)MathF.Round(center.Y / (float)arr.Length);

            return center;
        }

        private Color FindMostPrevalentColor(Bitmap b) {
            float closestDistance = float.MaxValue;
            Color fixedColor = Color.White;

            for (int y = 0; y < b.Height; y+= 4) {
                if (y >= b.Height) {
                    continue;
                }

                for (int x = 0; x < b.Width; x += 4) {
                    if (x >= b.Width) {
                        continue;
                    }

                    Color bColor = b.GetPixel(x, y);
                    if (bColor.R == 0 && bColor.G == 0 && bColor.B == 0) {
                        continue;
                    }
                    
                    for (int k = 0; k < colorMatchers.Count; k++) {
                        float distance = bColor.Distance(colorMatchers[k]);
                        if (distance < closestDistance) {
                            closestDistance = distance;
                            fixedColor = fixedColors[k];
                        }
                    }
                }
            }

            return fixedColor;
        }
    }
}

public static class Extensions {
    public static float Distance(this Color a, Color b) {
        return MathF.Sqrt(
            MathF.Pow(a.R - b.R, 2) +
            MathF.Pow(a.G - b.G, 2) +
            MathF.Pow(a.B - b.B, 2)
        );
    }
}