using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Caliper_Gradient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //////////////////////            Ảnh                           /////////////////////
            Mat source = new Mat("C:\\Users\\Admin\\Documents\\[SP]Visual Studio 2019\\Caliper_Image\\dulieu3\\31.png");
            //Mat source = new Mat("31.png");


            /////////////////////     Toạ độ ROI:   p1, p2, p3, p4        ///////////////////////
            int[] ROI = new int[8] { 52, 266, 248, 270, 57, 67, 253, 71 };


            /////////////////////      Options    ///////////////////////////////////////////////
            int gap = 15;                   //Khoảng khe hở giữa các đường
            int thresh = 75;                //Ngưỡng cắt của đường đạo hàm
            // "InsideToOutside" "OutsideToInside"
            string direction = "InsideToOutside";          //Hướng bắt điểm: trong ra ngoài, ngoài vào trong
            // "BestPoint" "MedianPoint"
            string mode = "BestPoint";                   //Kiểu bắt điểm: lấy điểm gần/xa nhất, lấy điểm trung vị
            Options options = new Options(gap, thresh, direction, mode);


            Gadgets.Caliper(source, ROI, options);


            Cv2.ImShow("Original Image", source);
            Cv2.WaitKey(0);
        }
    }
}
