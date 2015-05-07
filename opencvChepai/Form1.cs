using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.Util;
using System.IO.Ports;
using System.Runtime.InteropServices;


namespace opencvChepai
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            String[] ports = SerialPort.GetPortNames();
            if (ports.Length > 0)
            {
                serialPort1.PortName = ports[0];
                serialPort1.Open();
                this.Text = "open";
            }
        }
        Image<Bgr, Byte> img;
        int a;
        private void button1_Click(object sender, EventArgs e)
        {
            //CvInvoke.cvCreateCameraCapture(2);
            Capture capture = new Capture();
            capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 1280);
            capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 720);
            //capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS, 60);

            label1.Text = "Width:" + Convert.ToInt32(capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH)).ToString()
                + ",Height:" + Convert.ToInt32(capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT)).ToString();
            Application.Idle += new EventHandler(delegate(object sender2, EventArgs e2)
            {
                img = capture.QueryFrame();
                camerabox1.Image = img.ToBitmap();
                if (a > 0)
                {
                    getBox(img);
                    a = 0;
                }
            });
        }

        public Rectangle getBox(Image<Bgr, Byte> img)
        {
            Image<Bgr, Byte> simage = img;    //new Image<Bgr, byte>("license-plate.jpg");
            //Image<Bgr, Byte> simage = sizeimage.Resize(400, 300, Emgu.CV.CvEnum.INTER.CV_INTER_NN);
            Image<Gray, Byte> GrayImg = new Image<Gray, Byte>(simage.Width, simage.Height);
            IntPtr GrayImg1 = CvInvoke.cvCreateImage(simage.Size, Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
            //灰度化
            CvInvoke.cvCvtColor(simage.Ptr, GrayImg1, Emgu.CV.CvEnum.COLOR_CONVERSION.BGR2GRAY);
            //首先创建一张16深度有符号的图像区域
            IntPtr Sobel = CvInvoke.cvCreateImage(simage.Size, Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_16S, 1);
            //X方向的Sobel算子检测
            CvInvoke.cvSobel(GrayImg1, Sobel, 2, 0, 3);
            IntPtr temp = CvInvoke.cvCreateImage(simage.Size, Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
            CvInvoke.cvConvertScale(Sobel, temp, 0.00390625, 0);
            ////int it = ComputeThresholdValue(GrayImg.ToBitmap());
            ////二值化处理
            ////Image<Gray, Byte> dest = GrayImg.ThresholdBinary(new Gray(it), new Gray(255));
            Image<Gray, Byte> dest = new Image<Gray, Byte>(simage.Width, simage.Height);
            //二值化处理
            CvInvoke.cvThreshold(temp, dest, 0, 255, Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU);
            IntPtr temp1 = CvInvoke.cvCreateImage(simage.Size, Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 1);
            Image<Gray, Byte> dest1 = new Image<Gray, Byte>(simage.Width, simage.Height);
            CvInvoke.cvCreateStructuringElementEx(3, 1, 1, 0, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT, temp1);
            CvInvoke.cvDilate(dest, dest1, temp1, 6);
            CvInvoke.cvErode(dest1, dest1, temp1, 7);
            CvInvoke.cvDilate(dest1, dest1, temp1, 1);
            CvInvoke.cvCreateStructuringElementEx(1, 3, 0, 1, Emgu.CV.CvEnum.CV_ELEMENT_SHAPE.CV_SHAPE_RECT, temp1);
            CvInvoke.cvErode(dest1, dest1, temp1, 2);
            CvInvoke.cvDilate(dest1, dest1, temp1, 2);
            IntPtr dst = CvInvoke.cvCreateImage(simage.Size, Emgu.CV.CvEnum.IPL_DEPTH.IPL_DEPTH_8U, 3);
            CvInvoke.cvZero(dst);
            //dest.Dilate(10);
            //dest.Erode(5);
            Rectangle box = new Rectangle(0, 0, 0, 0);
            using (MemStorage stor = new MemStorage())
            {
                Contour<Point> contours = dest1.FindContours(
                    Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                    Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_CCOMP,
                    stor);
                for (; contours != null; contours = contours.HNext)
                {
                    box = contours.BoundingRectangle;
                    //Image<Bgr, Byte> test = simage.CopyBlank();
                    //test.SetValue(255.0);
                    double whRatio = (double)box.Width / box.Height;
                    int area = (int)box.Width * box.Height;
                    if (area > 1000 && area < 10000)
                    {
                        if ((3.0 < whRatio && whRatio < 4))
                        {
                            //test.Draw(box, new Bgr(Color.Red), 2);
                            //simage.Draw(box, new Bgr(Color.Red), 2);//CvInvoke.cvNamedWindow("dst");
                            var img2 = cut(img, box);
                            chepaibox1.Image = img2.ToBitmap();
                            MessageBox.Show(whRatio.ToString());
                            break;
                        }
                    }
                }
            }
            //box.X += 2;
            //box.Y += 2;
            //box.Width -= 6;
            //box.Height -= 6;
            return box;
        }

        public Image<Bgr, Byte> cut(Image<Bgr, byte> image, Rectangle rectangle)
        {
            //以矩形裁切图片
            var frame = image;//.Convert<Gray, Byte>();
            frame.ROI = rectangle;
            Image<Bgr, Byte> cutimg = new Image<Bgr, Byte>(rectangle.Width, rectangle.Height, new Bgr(255,255,255));
            CvInvoke.cvCopy(frame.Ptr, cutimg.Ptr, IntPtr.Zero);
            return frame;
        }

        public void hist(Image<Gray, Byte> img){
            Byte[, ,] data = img.Data;
            int[] hist = new int[img.Width];

            //绘制直方图
            Image<Bgr, Byte> imageHist = new Image<Bgr, Byte>(img.Width, 255, new Bgr(255d, 255d, 255d));
            Bgr black = new Bgr(0d, 0d, 0d);
            for (int i = 0; i < img.Width; i++)
            {
                int s = 0;
                for (int j = 0; j < img.Height; j++)
                {
                    if (data[j , i, 0] > 120) s++;
                }
                if (s > 5) hist[i] = (Byte)(s); //滤除噪声
            }

            for (int i = 0; i < img.Width; i++)
            {
                if (hist[i] > 0)
                {
                    int yuzhi = 8; //设定阈值
                    int k = 0;
                    for (int j = 0; j < yuzhi && i + j < img.Width; j++)
                    {
                        if (hist[i + j] == 0) k = 1;
                    }
                    if (k == 0)
                    {
                        while (hist[i] > 0 && i < img.Width-1) i++;
                    }
                    else
                    {
                        while (hist[i] > 0)
                        {
                            hist[i] = 0;
                            i++;
                        }
                    }
                }
            }

            for (int i = 0; i < img.Width; i++)
            {
                LineSegment2D line = new LineSegment2D(new Point(i, 255), new Point(i, 255 - hist[i]));
                if (hist[i] > 0) imageHist.Draw(line, black, 1);
            }
            histbox1.Image = imageHist.ToBitmap();
            //绘制并显示

            int[] hist2 = new int[img.Height];
            for (int i = 0; i < img.Height; i++)
            {
                int s = 0;
                for (int j = 0; j < img.Width; j++)
                {
                    if (data[i, j , 0] > 120) s++;
                }
                if (s > 24 && s < 90) hist2[i] = (Byte)(s); //滤除噪声
            }

            Image<Bgr, Byte> imageHist2 = new Image<Bgr, Byte>(255, img.Height, new Bgr(255d, 255d, 255d));
            for (int i = 0; i < img.Height; i++)
            {
                LineSegment2D line = new LineSegment2D(new Point(0, i), new Point(hist2[i], i));
                if (hist2[i] > 0) imageHist.Draw(line, black, 1);
            }
            histBox2.Image = imageHist.ToBitmap();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            a = 1;
            //Image<Bgr, Byte> img = new Image<Bgr, byte>("1.jpg");
            //imageBox1.Image = img.ToBitmap();
            //getBox(img);

            if (serialPort1.IsOpen)
            {
                byte[] data1 = { 0xDA, 0xAD, 0x88, 0x27 };
                byte[] data2 = { 0xDB, 0xBD };
                String asd = "粤B OK999 ";
                System.Text.Encoding utf8, gb2312;
                utf8 = System.Text.Encoding.GetEncoding("utf-8");
                gb2312 = System.Text.Encoding.GetEncoding("gb2312");
                byte[] chepai = System.Text.Encoding.Convert(utf8, gb2312, utf8.GetBytes(asd));

                byte[] send = new byte[data1.Length + data2.Length + chepai.Length];
                for (int i = 0; i < data1.Length; i++) send[i] = data1[i];
                for (int i = 0; i < chepai.Length; i++) send[i + data1.Length] = chepai[i];
                for (int i = 0; i < data2.Length; i++) send[i + chepai.Length + data1.Length] = data2[i];
                for (int i = 0; i < send.Length; i++) textBox1.Text += Convert.ToString(send[i], 16).ToUpper() + " ";
                serialPort1.Write(send, 0, send.Length);

            }
        }

        int jpg = 1;
        private void button3_Click(object sender, EventArgs e)
        {
            Image<Bgr, Byte> img = new Image<Bgr, byte>(jpg.ToString() + ".jpg");
            if (jpg < 4) jpg++;
            camerabox1.Image = img.ToBitmap();
            Rectangle box = getBox(img);
            var img2 = cut(img, box);
            chepaibox1.Image = img2.ToBitmap();
            var img3 = img2.Convert<Gray, Byte>();
            hist(img3); 
            
        }

        private void imageBox1_Click(object sender, EventArgs e)
        {

        }

        private void imageBox2_Click(object sender, EventArgs e)
        {

        }

    }
}