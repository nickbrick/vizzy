using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace vizzy
{
    public class Visualization
    {
        private float _systemScaling = 1 / 96f;
        public int Cols;
        private int Width;
        public int Height;
        public long VisOffset;
        public double ScrollOffset;
        public double Scale;
        public byte[] Data;
        private BitArray BitData;
        public bool UseMSB0;
        public PixelFormat PixelFormat;
        public ImageSnappingToPixels.Bitmap Img;

        public ScrollViewer ScrollViewer;
        public Canvas imgWrapper;
        public Label lblVisEnd;

        public event EventHandler ImageUpdated;
        public event EventHandler<ImageClickedArgs> ImageClicked;
        public EventArgs e = null;
        public delegate void EventHandler(Visualization v, EventArgs e);
        public delegate void EventHandler<ImageClickedArgs>(Visualization v, ImageClickedArgs e);

        public class ImageClickedArgs : EventArgs
        {
            public long ClickedOffset;
            public ImageClickedArgs(long o)
            {
                ClickedOffset = o;
            }
        }

        public void OnImageUpdated()
        {
            EventHandler handler = ImageUpdated;
            if (null != handler) handler(this, EventArgs.Empty);
        }
        public void OnImageClicked(ImageClickedArgs e)
        {
            EventHandler<ImageClickedArgs> handler = ImageClicked;
            if (null != handler) handler(this, e);
        }

        private static ImageBrush MakeAlphaBackground()
        {
            var alpha_background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/vizzy;component/Resources/back.bmp")));
            alpha_background.AlignmentY = AlignmentY.Top;
            alpha_background.AlignmentX = AlignmentX.Left;
            alpha_background.Stretch = Stretch.None;
            alpha_background.TileMode = TileMode.Tile;
            alpha_background.Viewport = new Rect(0, 0, 12, 12);
            alpha_background.Viewbox = new Rect(0, 0, 0, 0);
            alpha_background.ViewportUnits = BrushMappingMode.Absolute;
            return alpha_background;
        }
        private List<Brush> Backgrounds = new List<Brush> { Brushes.Black, MakeAlphaBackground() };

        public Visualization()
        {
            Cols = 16;
            Width = 16;
            PixelFormat = PixelFormats.Gray8;
            UseMSB0 = false;
            Scale = 1;
            InitScrollViewer();
            InitVisEnd();
            InitImg();
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                _systemScaling *= graphics.DpiX;
            }
        }


        public Visualization(string path) : this()
        {
            LoadData(path);
            UpdateImg();
        }
        private void InitScrollViewer()
        {
            ScrollViewer = new ScrollViewer();
            ScrollViewer.Margin = new System.Windows.Thickness(0);
            ScrollViewer.PanningMode = PanningMode.Both;
            ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            ScrollViewer.Background = Backgrounds[0];
            ScrollViewer.PreviewKeyDown += ScrollViewer_PreviewKeyDown;
            ScrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            ScrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            imgWrapper = new Canvas()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(0xff, 0x1e, 0x1e, 0x1e))
            };
            ScrollViewer.Content = imgWrapper;
        }
        private void InitImg()
        {
            Img = new ImageSnappingToPixels.Bitmap();
            RenderOptions.SetBitmapScalingMode(Img, BitmapScalingMode.NearestNeighbor);
            Img.IsHitTestVisible = true;
            imgWrapper.Children.Add(Img);
            imgWrapper.Children.Add(lblVisEnd);
            Img.PreviewMouseLeftButtonUp += Img_PreviewMouseLeftButtonUp;
        }
        private void InitVisEnd()
        {
            lblVisEnd = new Label
            {
                Content = "Row limit reached",
                Background = new SolidColorBrush(Color.FromArgb(0xff, 0x1e, 0x1e, 0x1e)),
                Foreground = new SolidColorBrush(Color.FromArgb(0xff, 0x92, 0xca, 0xf4)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment= VerticalAlignment.Top
            };
        }

        private void Img_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(Img);
            int x = (int)Math.Floor(pos.X * _systemScaling);
            int y = (int)Math.Floor(pos.Y * _systemScaling);
            if (x > Cols - 1) x = Cols - 1;
            long pixel_ord = y * Cols + x;
            long offset = VisOffset + pixel_ord * PixelFormat.BitsPerPixel / 8;
            if (ImageClicked != null)
            {
                ImageClicked(this, new ImageClickedArgs(offset));
            }
            Debug.WriteLine(offset);
        }
        public void LoadData(string path)
        {
            using (System.IO.FileStream fs = System.IO.File.Open(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                int numBytesToRead = Convert.ToInt32(fs.Length);
                Data = new byte[(numBytesToRead)];
                fs.Read(Data, 0, numBytesToRead);
            }
            BakeBitData();
        }
        private void BakeBitData()
        {
            Debug.WriteLine("baking bit data..");
            BitData = new BitArray(Data);
        }
        private BitmapSource MakeBitmap()
        {
            if (VisOffset < 0) VisOffset = 0;
            int subarray_length = (int)(Data.Length - VisOffset);
            //int subarray_length = (int)Math.Min((long)Math.Pow(512, 2), Data.Length - VisOffset) ;
            byte[] subarray = new byte[subarray_length];

            Array.ConstrainedCopy(Data, (int)VisOffset, subarray, 0, subarray_length);
            subarray = PaddedSubarray(subarray);
            int stride = GetStride(Cols, PixelFormat.BitsPerPixel);
            try
            {
                BitmapSource bitmapSource = BitmapSource.Create(Width, Height, 10, 10, PixelFormat,
                    BitmapPalettes.WebPalette, subarray, stride);
                return bitmapSource;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private byte[] PaddedSubarray(byte[] input)
        {
            byte[] inputpadded = input;
            int bpp = PixelFormat.BitsPerPixel;
            int Binput = input.Length;
            int pxinput = Math.Max(Binput * 8 / bpp, 1);
            int c = Cols;
            int h = (int)Math.Ceiling((float)pxinput / (float)c);
            int bc = c * bpp;
            int Bc = bc / 8;
            int stride = GetStride(c, bpp);
            int bstride = stride * 8;
            int bpad = bstride - bc;
            int pxpad = bpad / bpp;
            int pxstride = c + pxpad;
            int pxoutput = h * pxstride;
            int Boutput = pxoutput * bpp / 8;
            int inputpad = (stride - (Binput % Bc)) % stride;
            int Binputpadded = Binput + inputpad;

            Width = pxstride;
            Height = h;
            if (true)
            {
                if (bpp >= 8) //padding in integer amount of bytes
                {
                    inputpadded = new byte[Binputpadded];
                    Array.Copy(input, inputpadded, Binput); //copy original input into a larger array that divides evenly into h
                    Height = h;
                    byte[] output = new byte[Boutput];
                    for (int r = 0; r < h; r++)
                    {
                        if (r > 32767)
                        {
                            int x = 0;
                        }
                        byte[] row = new byte[stride];
                        Array.Copy(inputpadded, r * Bc, row, 0, Bc);
                        Array.Copy(row, 0, output, r * stride, stride);
                    }
                    Debug.WriteLine(String.Format("Binput: {0} \n pxoutput: {1}\n h: {2}\nstride: {3}\n", Binput, pxoutput, h, stride));
                    return output;
                }
                else if (bpp < 8) // bit padding
                {
                    BitArray barrinputpadded = new BitArray(bc * h);
                    int datalength = (int)(Data.Length - VisOffset) * 8;
                    for (int i = 0; i < datalength; i++)
                    {
                        barrinputpadded[i] = BitData[(int)VisOffset * 8 + i];
                    }
                    BitArray barroutput = new BitArray(stride * 8 * h);
                    bstride = stride * 8;
                    BitArray row = new BitArray(bstride);
                    byte[] output = new byte[barroutput.Length / 8];

                    for (int r = 0; r < h; r++)
                    {
                        for (int i = 0; i < bstride; i++)
                        {
                            if (i < bc)
                            {
                                int I_in = r * bc + i;
                                int I_out;
                                if (!UseMSB0)
                                {
                                    I_out = (i / 8) * 8 + 7 - i % 8;
                                }
                                else
                                {
                                    I_out = i;
                                }
                                if (I_in < barrinputpadded.Length)
                                    row[I_out] = barrinputpadded[I_in];
                            }
                        }
                        byte[] row_bytes = new byte[bstride / 8];
                        row.CopyTo(row_bytes, 0);
                        row_bytes.CopyTo(output, r * stride);
                    }
                    return output;
                }
            }
            return input;
        }
        private int GetStride(int w, int bpp)
        {
            if (bpp == 24) return w * 3;
            else if (bpp == 48) return w * 6;
            else return ((((w * bpp) - 1) / 32) + 1) * 4;
        }

        public bool SetCols(int w)
        {
            var oldCols = Cols;
            try
            {
                Cols = w;
                UpdateImg();
                return true;
            }
            catch (Exception x)
            {
                Cols = oldCols;
                Debug.WriteLine(x.ToString() + " @ SetCols(" + w.ToString() + ")");
                return false;
            }
        }
        public bool SetPixel(PixelFormat pf)
        {
            try
            {
                PixelFormat = pf;
                UpdateImg();
                return true;
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.ToString() + " @ SetPixel(" + pf.ToString() + ")");
                return false;
            }
        }
        public void UpdateImg()
        {
            Img.Source = MakeBitmap();
            Img.InvalidateMeasure();
            Img.InvalidateVisual();
            ClipImg();
            if (ImageUpdated != null)
            {
                ImageUpdated(this, e);
            }
        }

        public void ClipImg()
        {
            Img.Clip = new RectangleGeometry(new Rect(0, 0, Cols * Scale, Math.Min(Height * Scale, 0x8000) / _systemScaling));
            if (Height >= 0x8000)
            {
                lblVisEnd.Margin = new Thickness(0, 0x8000 * Scale / _systemScaling, 0, 0);
                lblVisEnd.Visibility = Visibility.Visible;
                lblVisEnd.RenderTransform = new ScaleTransform(1/Scale, 1/Scale);
            }
            imgWrapper.Width = Cols * Scale / _systemScaling;
            imgWrapper.Height = Height * Scale / _systemScaling;
        }

        public void SwitchBackground(int i)
        {
            ScrollViewer.Background = Backgrounds[i];
        }

        private void ScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) || (Keyboard.Modifiers == ModifierKeys.Shift))
            {
                ScrollOffset = ScrollViewer.VerticalOffset / ScrollViewer.ScrollableHeight;
                Debug.WriteLine("offset = " + ScrollOffset);
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    Scale *= Math.Pow(2, 0.25);
                    var scaleTransform = new ScaleTransform(Scale, Scale);
                    Img.RenderTransform = scaleTransform;
                    imgWrapper.LayoutTransform = scaleTransform;
                }
                else if (e.Delta < 0)
                {
                    Scale /= Math.Pow(2, 0.25);
                    if (Scale < 1) Scale = 1;
                    var scaleTransform = new ScaleTransform(Scale, Scale);
                    Img.RenderTransform = scaleTransform;
                    imgWrapper.LayoutTransform = scaleTransform;
                }
                ClipImg();
                if (ImageUpdated != null)
                {
                    ImageUpdated(this, e);
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                (sender as ScrollViewer).ScrollToVerticalOffset((sender as ScrollViewer).ContentVerticalOffset);
                (sender as ScrollViewer).ScrollToHorizontalOffset((sender as ScrollViewer).ContentHorizontalOffset - e.Delta);
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) || (Keyboard.Modifiers == ModifierKeys.Shift))
                try
                {
                    ScrollViewer.ScrollToVerticalOffset(ScrollOffset * ScrollViewer.ScrollableHeight);
                }
                catch (Exception x)
                {
                    Debug.WriteLine(x);
                }
        }
    }
}
