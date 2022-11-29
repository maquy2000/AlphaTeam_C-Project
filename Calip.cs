using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Caliper_Gradient
{
    public static class Gadgets
    {
        public static void Caliper(Mat Source, int[] ROI, Options Opts)
        {
            #region      Khởi tạo các điểm và list điểm phục vụ vẽ ROI
            // Các điểm base
            Point point1 = new Point(ROI[0], ROI[1]);
            Point point2 = new Point(ROI[2], ROI[3]);
            Point point3 = new Point(ROI[4], ROI[5]);
            Point point4 = new Point(ROI[6], ROI[7]);
            Point pointCenterStart = new Point((int)(point1.X + point3.X) / 2, (int)(point1.Y + point3.Y) / 2);
            Point pointCenterEnd = new Point((int)(point2.X + point4.X) / 2, (int)(point2.Y + point4.Y) / 2);

            // Xác định góc của ROI
            double angle = 0;
            int x = pointCenterEnd.X - pointCenterStart.X;
            int y = pointCenterEnd.Y - pointCenterStart.Y;
            if (x > 0) { angle = 180 / Math.PI * Math.Atan2(y, x); }
            else if (x < 0) { angle = 180 + 180 / Math.PI * Math.Atan2(y, x); }
            else if (x == 0 & y < 0) { angle = -90; }
            else if (x == 0 & y > 0) { angle = 90; }

            //Tạo list các điểm thuộc 3 đường ngang chính
            //Khởi tạo điểm đầu của list là điểm đầu của các đường ngang
            List<Point> ListPoint12 = new List<Point>() { new Point(point1.X, point1.Y) };
            List<Point> ListPoint34 = new List<Point>() { new Point(point3.X, point3.Y) };
            List<Point> ListPointCenter = new List<Point>() { new Point(pointCenterStart.X, pointCenterStart.Y) };

            // Tính độ dài của ROI
            double widthROI = Math.Sqrt(Math.Pow(pointCenterEnd.X - pointCenterStart.X, 2) + Math.Pow(pointCenterEnd.Y - pointCenterStart.Y, 2));
            // Tìm số lượng point trên các đường ngang chính theo độ dài ROI
            int numPoint = (int)widthROI / Opts.Gap + 1;
            //Console.WriteLine("{0}, {1}", widthROI, numPoint);
            // Add thêm các điểm vào list
            for (int i = 1; i < numPoint; i++)          //Chạy từ 1 bởi điểm đầu tiên 0 đã được tạo
            {
                Point eachPoint12 = RotatePoint(new Point(i * Opts.Gap, 0), new Point(0, 0), angle, point1);
                Point eachPoint34 = RotatePoint(new Point(i * Opts.Gap, 0), new Point(0, 0), angle, point3);
                Point eachPointCenter = RotatePoint(new Point(i * Opts.Gap, 0), new Point(0, 0), angle, pointCenterStart);

                ListPoint12.Add(eachPoint12);
                ListPoint34.Add(eachPoint34);
                ListPointCenter.Add(eachPointCenter);
                //Console.WriteLine("({0}, {1})", ListPointCenter[i].X, ListPointCenter[i].Y);
            }
            #endregion


            #region       Load ảnh và biến đổi ảnh
            // Tạo ảnh xám
            Mat ImgGray = new Mat();
            Cv2.CvtColor(Source, ImgGray, ColorConversionCodes.BGR2GRAY);

            // Lấy matrix sobel tương ứng theo góc ROI
            var data = SobelMatrix.GetSobelMatrix(angle);
            //var data = new int[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };    //Test
            // Gán matrix sobel thành kernel 
            var kernel = new Mat(rows: 3, cols: 3, type: MatType.CV_32SC1, data);

            // In kernel ra màn hình
            for (int i = 0; i<3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var nkernel = kernel.Get<int>(i, j);
                    Console.Write("{0}    ", nkernel);
                }
                Console.WriteLine("\n");
            }

            //Tạo và hiển thị ảnh đạo hàm bậc 1 2 theo ma trận kernel
            Mat ImgSobel1 = new Mat();
            Mat ImgSobel2 = new Mat();

            Cv2.Filter2D(ImgGray, ImgSobel1, MatType.CV_64FC1, kernel);
            Cv2.Filter2D(ImgSobel1, ImgSobel2, MatType.CV_64FC1, kernel);

            //Cv2.ImShow("Gray", ImgGray);         //Hiển thị ảnh xám
            //Cv2.ImShow("Sobel 1", ImgSobel1);
            //Cv2.ImShow("Sobel 2", ImgSobel2);

            #endregion


            // Vẽ các đường bao của ROI
            DrawROI(Source, point1, point2, point3, point4, pointCenterStart, pointCenterEnd);

            // Vẽ các đường thẳng bên trong ROI
            DrawROIEdges(Source, ListPoint12, ListPoint34, numPoint);

            // Lấy list các đường pixel
            List<List<Point>> ListRangePix12 = GetListRangePixel(Source, ListPointCenter, ListPoint12, angle, numPoint, Opts);
            List<List<Point>> ListRangePix34 = GetListRangePixel(Source, ListPointCenter, ListPoint34, angle, numPoint, Opts);


            List<List<double>> list_derivative1_12 = GetListLineInImgSobel(ImgSobel1, ListRangePix12);
            List<List<double>> list_derivative2_12 = GetListLineInImgSobel(ImgSobel2, ListRangePix12);
        }


        /// <summary>
        /// Hàm sử dụng để xoay các điểm point quanh điểm pointCenter với góc angle, sau đó cộng offset là khoảng cách tới gốc thực
        /// </summary>
        public static Point RotatePoint(Point point, Point pointCenter, double angle, Point offset)
        {
            double x = pointCenter.X + (point.X - pointCenter.X) * Math.Cos(angle * Math.PI /180) 
                - (point.Y - pointCenter.Y) * Math.Sin(angle * Math.PI / 180) + offset.X;
            double y = pointCenter.Y + (point.X - pointCenter.X) * Math.Sin(angle * Math.PI / 180)
                + (point.Y - pointCenter.Y) * Math.Cos(angle * Math.PI / 180) + offset.Y;
            return new Point((int)x, (int)y);
        }


        /// <summary>
        /// Vẽ các đường thẳng chính của ROI
        /// </summary>
        public static void DrawROI(Mat Source, Point point1, Point point2, Point point3, Point point4, Point pointCenter_Start, Point pointCenter_End)
        {
            Scalar colorROILine = new Scalar(200, 200, 27);

            Cv2.Line(Source, point1, point2, colorROILine, 2);
            Cv2.Line(Source, point1, point3, colorROILine, 2);
            Cv2.Line(Source, point4, point2, colorROILine, 2);
            Cv2.Line(Source, point4, point3, colorROILine, 2);

            Cv2.Line(Source, pointCenter_Start, pointCenter_End, colorROILine, 2);
        }


        /// <summary>
        /// Vẽ các đường thẳng bắt điểm của ROI
        /// </summary>
        public static void DrawROIEdges(Mat Source, List<Point> ListPoint12, List<Point> ListPoint34, int numPoint)
        {
            Scalar colorROIEdge = new Scalar(0, 255, 0);
            for (int i = 0; i < numPoint; i++)
            {
                Cv2.Line(Source, ListPoint12[i], ListPoint34[i], colorROIEdge, 1);
            }
        }


        /// <summary>
        /// Hàm trả về list các đường thẳng
        /// Mỗi đường thẳng này chứa các pixel của đường bắt điểm
        /// </summary>
        public static List<List<Point>> GetListRangePixel(Mat Source, List<Point> ListPointStart, List<Point> ListPointEnd, double angle, int numPoint, Options Options)
        {
            //Options.Direction
            //Console.WriteLine(Options.Direction);
            List<List<Point>> ListRangePix = new List<List<Point>>();

            //Tìm phương trình đường thẳng ax + by + c = 0
            double a = ListPointEnd[0].Y - ListPointStart[0].Y;
            double b = -(ListPointEnd[0].X - ListPointStart[0].X);
            for (int i = 0; i < numPoint; i++)
            {
                double c = -ListPointStart[i].X * ListPointEnd[i].Y + ListPointStart[i].Y * ListPointEnd[i].X;
                //Tạo một rangepix, là một list chứa các vị trí pixel
                List<Point> RangePix = new List<Point>();

                if ((angle >= -45 & angle <= 45) || (angle >= 135 & angle <= 225))
                {
                    if (a > 0)
                    {
                        for (int y = ListPointStart[i].Y; y <= ListPointEnd[i].Y; y++)
                        {
                            double x = -(b * y + c) / a;
                            if (x >= 0 & x <= Source.Width)
                            {
                                RangePix.Add(new Point(x, y));
                                //Cv2.Circle(Source, new Point((int)x, y), 2, Scalar.Red, 1);
                            }
                            else { break; }
                        }
                    }
                    else
                    {
                        for (int y = ListPointStart[i].Y; y >= ListPointEnd[i].Y; y--)
                        {
                            double x = -(b * y + c) / a;
                            if (x >= 0 & x <= Source.Width)
                            {
                                RangePix.Add(new Point(x, y));
                                //Cv2.Circle(Source, new Point((int)x, y), 2, Scalar.Red, 1);
                            }
                            else { break; }
                        }
                    }
                }

                else
                {
                    if (b < 0)
                    {
                        for (int x = ListPointStart[i].X; x <= ListPointEnd[i].X; x++)
                        {
                            double y = -(a * x + c) / b;
                            if (y >= 0 & x <= Source.Height)
                            {
                                RangePix.Add(new Point(x, y));
                                //Cv2.Circle(Source, new Point(x, (int)y), 2, Scalar.Red, 1);
                            }
                            else { break; }
                        }
                    }
                    else
                    {
                        for (int x = ListPointStart[i].X; x >= ListPointEnd[i].X; x--)
                        {
                            double y = -(a * x + c) / b;
                            if (y >= 0 & x <= Source.Height)
                            {
                                RangePix.Add(new Point(x, y));
                                //Cv2.Circle(Source, new Point(x, (int)y), 2, Scalar.Red, 1);
                            }
                            else { break; }
                        }
                    }
                }

                // Mặc định lấy các pixel từ trong ra ngoài, nếu là ngoài vào trong thì đảo lại
                if (Options.Direction == "OutsideToInside") { RangePix.Reverse(); }
                ListRangePix.Add(RangePix);
            }

            return ListRangePix;
        }


        /// <summary>
        /// Hàm trả về list các line
        /// Mỗi line là các giá trị độ xám trên các đường bắt điểm
        /// </summary>
        public static List<List<double>> GetListLineInImgSobel(Mat ImgSobel, List<List<Point>> ListRangePix)
        {
            List<List<double>> ListLineSobel = new List<List<double>>();

            for (int i = 0; i < ListRangePix.Count; i++)
            {
                List<double> LineSobel = new List<double>();
                for (int j = 0; j < ListRangePix[i].Count; j++)
                {
                    var ngrayness = ImgSobel.Get<double>(ListRangePix[i][j].Y, ListRangePix[i][j].X);
                    LineSobel.Add(ngrayness);
                }
                ListLineSobel.Add(LineSobel);
            }
            return ListLineSobel;
        }


    }


    /// <summary>
    /// Option để thay đổi các thuộc tính của ứng dụng
    /// </summary>
    public class Options
    {
        public int Gap;
        public int Thresh;
        public string Direction;
        public string Mode;

        public Options(int gap, int thresh, string direction, string mode)
        {
            Gap = gap;
            Thresh = thresh;
            Direction = direction;
            Mode = mode;
        }
    }


    public static class SobelMatrix
    {
        static private int[,] MatrixHorizontal = new int[,]     //Ma trix nằm ngang
        {
            { -1, 0, 1 },
            { -2, 0, 2 },
            { -1, 0, 1 }
        };

        static private int[,] MatrixVertical = new int[,]        //Matrix đứng
        {
            { -1, -2, -1 },
            {  0,  0,  0 },
            {  1,  2,  1 },
        };

        static private int[,] MatrixCrossLeftTop = new int[,]    //Ma trix hướng trái trên -> phải dưới
        {
            { -2, -1,  0 },
            { -1,  0,  1 },
            {  0,  1,  2 },
        };

        static private int[,] MatrixCrossRightTop = new int[,]    //Ma trix hướng phải trên -> trái dưới
        {
            {  0, -1, -2 },
            {  1,  0, -1 },
            {  2,  1,  0 },
        };

        public static int[,] GetSobelMatrix(double angle)
        {
            int[,] matrix = new int[3, 3];
            if ((angle >= -22.5 & angle <= 22.5) | (angle >= 157.5 & angle <= 202.5))
            {
                matrix = MatrixVertical;
            }
            else if ((angle >= -67.5 & angle <= -22.5) | (angle >= 112.5 & angle <= 157.5))
            {
                matrix = MatrixCrossLeftTop;
            }
            else if ((angle >= 22.5 & angle <= 67.5) | (angle >= 202.5 & angle <= 247.5))
            {
                matrix = MatrixCrossRightTop;
            }
            else
            {
                matrix = MatrixHorizontal;
            }

            return matrix;
        }
    }
}
