using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ZumaBot2 {
    public partial class MainForm : Form {
        private bool foundGameWindow = false;
        private System.Drawing.Point? windowLocation = null;
        private Bitmap blueBallTemplate;
        private Bitmap gameWindow = new Bitmap(640, 480, PixelFormat.Format32bppArgb);
        private Thread captureThread;

        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            /*Bitmap screen = new Bitmap(640, 480, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(screen)) {
                g.CopyFromScreen(2922 % 1920, 577, 0, 0, screen.Size);
            }*/

            using var src = Cv2.ImRead(GetAssetPath("gamescreen.png"))//screen.ToMat()
                .CvtColor(ColorConversionCodes.RGB2HSV)
                .ExtractChannel(1)
                .Threshold(100, 255, ThresholdTypes.Binary);
            using var tpl = Cv2.ImRead(GetAssetPath("blue_ball.png")).ExtractChannel(0);
            using var dst = Cv2.ImRead(GetAssetPath("gamescreen.png"));

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

            /*
            Dictionary<string, int> selParams = new Dictionary<string, int>();
            selParams["p1"] = 100;
            selParams["p2"] = 100;
            selParams["minRadius"] = 0;
            selParams["maxRadius"] = 32;
            selParams["minDist"] = 1;
            selParams["dp"] = 1;


            var createCb = (string key) => new TrackbarCallbackNative((int v, IntPtr ptr) => {
                selParams[key] = v;

                CircleSegment[] circles = Cv2.HoughCircles(outp, HoughModes.Gradient, selParams["dp"] + 1, selParams["minDist"] + 1, selParams["p1"] + 1, selParams["p2"] + 1, selParams["minRadius"], selParams["maxRadius"]);
                var dstclone = dst.Clone();
                foreach (CircleSegment segment in circles) {
                    Cv2.Circle(dstclone, (int)segment.Center.X, (int)segment.Center.Y, (int)segment.Radius, new Scalar(0, 255, 0), 2);
                }

                Cv2.ImShow("out", dstclone);
            });
 
            new Window("Settings");
            Cv2.CreateTrackbar("p1", "Settings", 250, createCb("p1"));
            Cv2.CreateTrackbar("p2", "Settings", 250, createCb("p2"));
            Cv2.CreateTrackbar("minRadius", "Settings", 250, createCb("minRadius"));
            Cv2.CreateTrackbar("maxRadius", "Settings", 250, createCb("maxRadius"));
            Cv2.CreateTrackbar("minDist", "Settings", 250, createCb("minDist"));
            Cv2.CreateTrackbar("dp", "Settings", 250, createCb("dp"));

            Cv2.WaitKey();*/

            //Cv2.BitwiseAnd(dst, src.CvtColor(ColorConversionCodes.GRAY2BGR), dst);

            //KeyPoint[] keypoints= Cv2.FAST(src, 128);
            //Cv2.DrawKeypoints(src, keypoints, src, null, DrawMatchesFlags.DrawRichKeypoints);

            //using var msk = screen.ToMat().SetTo(new Scalar(55, 252, 70));
            //using var wht = screen.ToMat().SetTo(new Scalar(255, 255, 255));

            //src.Set(0, 5);

            //Cv2.InRange(src, msk.CvtColor(ColorConversionCodes.RGB2HSV), msk.CvtColor(ColorConversionCodes.RGB2HSV), dst);
            //Cv2.find(src, msk, dst);

            //Cv2.Blur(src, src, new OpenCvSharp.Size(5, 5));
            //Cv2.Sobel(src, src, MatType.CV_8UC1, 1, 0, 5);

            //CircleSegment[] circles = Cv2.HoughCircles(src, HoughModes.Gradient, 1f, src.Rows / 16f, 100, 100, 0, 128);
            //foreach (CircleSegment circle in circles) {
            //    Cv2.Circle(dst, (int) circle.Center.X, (int) circle.Center.Y, (int) circle.Radius, new Scalar(255f, 0f, 0f));
            //}
            //Cv2.CornerHarris(src, dst, 4, 3, 0.04f);
            //Cv2.Mahalanobis(src, dst, Cv2.CalcCovarMatrix(tpl, tpl, tpl, CovarFlags.Scrambled));

            //Cv2.InRange(src, InputArray.Create(new Vec3i(120, 67, 76)), InputArray.Create(new Vec3i(136, 49, 41)), dst);
            //using var src2 = new Mat();
            //Cv2.BitwiseAnd(src, src, src2, dst);
            //Cv2.MatchTemplate(src, tpl, dst,TemplateMatchModes.CCoeffNormed);
            //Cv2.Threshold(dst, dst, 0.5f, 1f, ThresholdTypes.Binary);
            /*using (new Window("src", src))
            using (new Window("out image", outp))
                //using (new Window("msk", msk))
                using (new Window("dst image", dst)) {
                Cv2.WaitKey();
            }*/

            //captureThread = new Thread(() => CaptureThread());
            //captureThread.Start();
        }

        private string GetAssetPath(string filename) {
            return Path.Combine(Directory.GetCurrentDirectory(), "images", filename);
        }

        private void CaptureThread() {
            while (true) {
                using (Graphics g = Graphics.FromImage(gameWindow)) {
                    g.CopyFromScreen(windowLocation.Value.X, windowLocation.Value.Y, 0, 0, new System.Drawing.Size(640, 480));
                }
            }
        }
    }
}