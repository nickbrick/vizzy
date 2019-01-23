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
        public int Cols;
        private int Width;
        public int Height;
        public long VisOffset;
        public double ScrollOffset;
        public double Scale;
        public byte[] Data;
        public PixelFormat PixelFormat;
        public Image Img;
        public ScrollViewer ScrollViewer;

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
            Scale = 1;
            InitScrollViewer();
            InitImg();
            
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
            //Grid.Column = "2" Grid.Row = "1" 
        }
        private void InitImg()
        {
            Img = new Image();
            Img.Margin = new System.Windows.Thickness(0);
            Img.Width = Double.NaN;
            Img.Height= Double.NaN;

            Img.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(Img, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(Img, EdgeMode.Aliased);
            Img.MinHeight = 200;
            Img.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            Img.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            Img.IsHitTestVisible = true;
            ScrollViewer.Content = Img;
            Img.PreviewMouseLeftButtonUp += Img_PreviewMouseLeftButtonUp;
        }

        private void Img_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(Img);
            int x = (int)Math.Floor(pos.X / Scale);
            int y = (int)Math.Floor(pos.Y / Scale);
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
            
        }

        private BitmapSource MakeBitmap()
        {

            byte[] subarray = new byte[Data.Length - VisOffset];
            
            if (VisOffset < 0) VisOffset = 0;
            Array.ConstrainedCopy(Data, (int)VisOffset, subarray, 0, Data.Length - (int)VisOffset);
            subarray = PaddedSubrray(subarray);
            int stride = GetStride(Cols, PixelFormat.BitsPerPixel);

            //Width = stride * 8 / PixelFormat.BitsPerPixel;
            try
            {
                BitmapSource bitmapSource = BitmapSource.Create(Width, Height, 10, 10, PixelFormat,
                    BitmapPalettes.WebPalette, subarray, stride);
                //Height = pixels / Width + 1;
                return bitmapSource;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private byte[] PaddedSubrray(byte[] input)
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
            int Bpad = stride - Bc;
            int bpad = Bpad * 8;
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

                    BitArray barrinput = new BitArray(input);
                    for (int i = 0; i < barrinput.Length; i++)
                    {
                        barrinputpadded[i] = barrinput[i];
                    }
                    BitArray barroutput = new BitArray(stride * 8 * h);
                    int bstride = stride * 8;
                    BitArray row = new BitArray(bstride);
                    byte[] output = new byte[barroutput.Length / 8];

                    for (int r = 0; r < h; r++)
                    {
                        for (int i = 0; i < bstride; i++)
                        {
                            if (i < bc)
                            {
                                int I_in = r * bc + i;
                                int I_out = r * bstride + i;
                                if (I_in < barrinputpadded.Length)
                                    row[i] = barrinputpadded[I_in];
                                //padded_bits[I_out] = input_bits[I_in];
                            }

                        }
                        BitArray wor = new BitArray(bstride); //reverse bit endiannes for each byte
                        for (int B = 0; B < bstride / 8; B++)
                        {
                            for (int u = 0; u < 8; u++)
                            {
                                wor[8 * B + u] = row[8 * B + 7 - u];
                            }

                        }

                        byte[] row_bytes = new byte[bstride / 8];
                        wor.CopyTo(row_bytes, 0);
                        //row.CopyTo(row_bytes, 0);
                        row_bytes.CopyTo(output, r * stride);
                    }
                    //byte[] padded = new byte[padded_bits.Length / 8];
                    //padded_bits.CopyTo(padded, 0);
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
            Img.Width = Width * Scale;
            ClipImg();
            if (ImageUpdated != null)
            {
                ImageUpdated(this, e);
            }
        }

        public void ClipImg()
        {
            Img.Clip = new RectangleGeometry(new Rect(0, 0, Cols * Scale, Height * Scale));
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
                    Scale *= 1.2;
                    Img.Width = Width * Scale;
                    Img.Height *= Scale;
                }
                else if (e.Delta < 0)
                {
                    Scale /= 1.2;
                    if (Scale < 1) Scale = 1;
                    Img.Width = Width * Scale;
                    Img.Height /= Scale;
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
