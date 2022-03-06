using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = OpenCvSharp.Point;

namespace ZumaBot2 {
    public partial class MainForm : Form {
        private Point windowLocation;
        private bool foundGameWindow = false;

        private Bitmap gameWindow = new Bitmap(640, 480, PixelFormat.Format32bppRgb);
        private Thread captureThread;

        private const int defaultFrameDelay = 16;

        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
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
            using var mFrogTemplate = new Mat(GetAssetPath("frog.png"));

            Dictionary<string, int> selParams = new Dictionary<string, int>();
            selParams["blockSize"] = 8;
            selParams["k"] = 1;

            using ORB orb = ORB.Create();
            KeyPoint[] kpO;
            using var oaO = new Mat();

            KeyPoint[] kpT;
            using var oaT = new Mat();

            orb.DetectAndCompute(mFrogTemplate, null, out kpT, oaT);
            orb.DetectAndCompute(original, null, out kpO, oaO);

            using var bfm = BFMatcher.Create("BruteForce-Hamming");
            var matches = bfm.Match(oaT, oaO);

            var orderedMatches = matches; //matches.OrderBy((k) => k.Distance).Take(10).ToArray();
            MessageBox.Show(orderedMatches.Length.ToString());

            using var mFinalOut = new Mat();
            Cv2.DrawMatches(mFrogTemplate, kpT, original, kpO, orderedMatches, mFinalOut);
            Cv2.ImShow("out2", mFinalOut);

            /*var createCb = (string key) => new TrackbarCallbackNative((int v, IntPtr ptr) => {
                selParams[key] = v;
            */


            //});

            /*new Window("Settings");
            Cv2.CreateTrackbar("blockSize", "Settings", 250, createCb("blockSize"));
            Cv2.CreateTrackbar("k", "Settings", 250, createCb("k"));*/

            Cv2.WaitKey(defaultFrameDelay);
        }

        private void CaptureThread() {
            using Graphics g = Graphics.FromImage(gameWindow);
            List<Color> colorMatchers = new List<Color>();
            List<Color> fixedColors = new List<Color>();

            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Red, "ct_red.png");
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Green, "ct_green.png");
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Blue, "ct_blue.png");
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Yellow, "ct_yellow.png");
            ColorDetectionLoadInto(colorMatchers, fixedColors, Color.Violet, "ct_violet.png");

            while (true) {
                g.CopyFromScreen(windowLocation.X, windowLocation.Y, 0, 0, gameWindow.Size);

                // Convert the game capture, but only pay attention to the saturation channel (balls are more saturated than the backgrounds)
                using var mGameOriginal = gameWindow.ToMat();
                using var mGameColor = gameWindow.ToMat();
                using var mGame = gameWindow
                    .ToMat()
                    .CvtColor(ColorConversionCodes.RGB2HSV)
                    .ExtractChannel(1)
                    .Threshold(100, 255, ThresholdTypes.Binary);


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
    }
}