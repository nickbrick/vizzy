using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace ImageSnappingToPixels
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class Window1 : System.Windows.Window
    {

        public Window1()
        {
            InitializeComponent();
        }

        public void ShiftContent(object sender, KeyEventArgs e)
        {
            double offsetAmount = 0.1;
            Vector offset = new Vector();
            if (e.Key == Key.Left)
                offset.X -= offsetAmount;
            else if (e.Key == Key.Right)
                offset.X += offsetAmount;
            else if (e.Key == Key.Up)
                offset.Y -= offsetAmount;
            else if (e.Key == Key.Down)
                offset.Y += offsetAmount;

            TranslateTransform layoutTransform = root.RenderTransform as TranslateTransform;
            if (layoutTransform == null)
                layoutTransform = new TranslateTransform(0, 0);
            
            layoutTransform.X += offset.X;
            layoutTransform.Y += offset.Y;

            root.RenderTransform = layoutTransform;

            root.InvalidateMeasure();
        }

    }
}