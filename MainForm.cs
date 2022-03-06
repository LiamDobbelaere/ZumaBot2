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

        private void Capture() {
            /*Bitmap screen = new Bitmap(640, 480, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(screen)) {
                g.CopyFromScreen(2922 % 1920, 577, 0, 0, screen.Size);
            }*/

            using var src = Cv2.ImRead(GetAssetPath("gamescreen_2.png"))//screen.ToMat()
                .CvtColor(ColorConversionCodes.RGB2HSV)
                .ExtractChannel(1)
                .Threshold(100, 255, ThresholdTypes.Binary);
            using var tpl = Cv2.ImRead(GetAssetPath("blue_ball.png")).ExtractChannel(0);
            using var dst = Cv2.ImRead(GetAssetPath("gamescreen_2.png"));

            Cv2.FilterSpeckles(src, 0, 16, 8);

            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(8, 8));
            Cv2.MorphologyEx(src, src, MorphTypes.Close, kernel);

            ConnectedComponents cc = Cv2.ConnectedComponentsEx(src, PixelConnectivity.Connectivity4);
            Mat outp = new Mat();
            cc.FilterByBlob(src, outp, cc.GetLargestBlob());

            Cv2.BitwiseAnd(dst, outp.CvtColor(ColorConversionCodes.GRAY2BGR), dst);

            Color[] colorMatchers = new Color[] {
                Color.FromArgb(223, 127, 105),
                Color.FromArgb(57, 255, 78),
                Color.FromArgb(49, 193, 254),
                Color.FromArgb(254, 142, 252),
                Color.FromArgb(255, 255, 24)
            };

            CircleSegment[] circles = Cv2.HoughCircles(outp, HoughModes.Gradient, 5, 20, 30, 35, 0, 20);
            var dstclone = dst.Clone();
            foreach (CircleSegment segment in circles) {
                int col = outp.At<int>((int)segment.Center.Y, (int)segment.Center.X);

                if (col < 0) {
                    Vec3b ccol = dst.At<Vec3b>((int)segment.Center.Y, (int)segment.Center.X);
                    Color ccolAsColor = Color.FromArgb(ccol[2], ccol[1], ccol[0]);
                    Color usedColor = Color.Orange;
                    float lowestDistance = float.MaxValue;
                    foreach (Color cs in colorMatchers) {
                        float colorDist = MathF.Sqrt(
                            MathF.Pow(ccolAsColor.R - cs.R, 2) +
                            MathF.Pow(ccolAsColor.G - cs.G, 2) +
                            MathF.Pow(ccolAsColor.B - cs.B, 2)
                        );

                        if (colorDist < lowestDistance) {
                            usedColor = cs;
                            lowestDistance = colorDist;
                        }
                    }


                    Cv2.Circle(dstclone, (int)segment.Center.X, (int)segment.Center.Y, (int)segment.Radius, new Scalar(usedColor.B, usedColor.G, usedColor.R), 2);
                }
            }

            Cv2.ImShow("out", dstclone);
            Cv2.WaitKey();
        }

        private void CaptureThread() {
            using Graphics g = Graphics.FromImage(gameWindow);
            Color[] colorMatchers = new Color[] {
                // Blues
                ColorTranslator.FromHtml("#40f9ff"),
                ColorTranslator.FromHtml("#3d3dbe"),

                // Greens
                ColorTranslator.FromHtml("#48fb67"),
                ColorTranslator.FromHtml("#388047"),

                // Yellows
                ColorTranslator.FromHtml("#fffe2d"),
                ColorTranslator.FromHtml("#a77c17"),

                // Violets
                ColorTranslator.FromHtml("#ff9dff"),
                ColorTranslator.FromHtml("#a613b2"),

                // Reds
                ColorTranslator.FromHtml("#dc7e6e"),
                ColorTranslator.FromHtml("#ae0812")
            };

            Color[] fixedColors = new Color[] {
                Color.Blue,
                Color.Blue,
                Color.Green,
                Color.Green,
                Color.Yellow,
                Color.Yellow,
                Color.Violet,
                Color.Violet,
                Color.Red,
                Color.Red
            };

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

                // Get rid of small noise speckles
                Cv2.FilterSpeckles(mGame, 0, 16, 8);
                
                // Fill holes
                using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(8, 8));
                Cv2.MorphologyEx(mGame, mGame, MorphTypes.Close, kernel);

                // Get largest connected component (the ball chain)
                ConnectedComponents cc = Cv2.ConnectedComponentsEx(mGame, PixelConnectivity.Connectivity4);
                using Mat mGameLargestBlob = new Mat();
                cc.FilterByBlob(mGame, mGameLargestBlob, cc.GetLargestBlob());

                // Create a composite of the previous stap (binary largest blob image + original color)
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
                        for (int k = 0; k < colorMatchers.Length; k++) {
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