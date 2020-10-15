using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

using SkiaSharp;
using SkiaSharp.Views.Forms;
using TouchTracking;

using nexus.core.logging;
using nexus.protocols.ble;
using nexus.protocols.ble.scan;

namespace Bruco
{
    public partial class MainPage : ContentPage
    {
        SKPoint Position = new SKPoint(0, 0);
        float centerX, centerY;
        float radius;

        public MainPage(IBluetoothLowEnergyAdapter bleAdapter)
        //public MainPage()
        {

            InitializeComponent();
            this.BackgroundColor = Color.Black;

            // set global binding context here
            // creates a single instance of the model for all tabbed pages!
            this.BindingContext = new ParametersModel(bleAdapter);

        }

        SKPaint blackStroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Black,
            StrokeWidth = 5,
            IsAntialias = true
        };

        SKPaint redFill = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Red, //.WithAlpha(0x20)
        };

        SKPaint textPaint = new SKPaint
        {
            Color = SKColors.RoyalBlue,
            TextSize = 20
        };

        void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            SKPoint tmp;
            float ratio;
            
            switch (args.Type)
            {
                case TouchActionType.Pressed:
                    tmp = ConvertToPixel(args.Location);
                    //keep it inside circle
                    ratio = (float)Math.Sqrt(tmp.X * tmp.X + tmp.Y * tmp.Y) / radius;
                    if (ratio <= 1) { Position = tmp; }
                    canvasView.InvalidateSurface();
                    break;

                case TouchActionType.Moved:
                    tmp = ConvertToPixel(args.Location);
                    //keep it inside circle
                    ratio = (float)Math.Sqrt(tmp.X * tmp.X + tmp.Y * tmp.Y) / radius;
                    if (ratio > 1) { tmp.X /= ratio; tmp.Y /= ratio; }
                    Position = tmp;
                    canvasView.InvalidateSurface();
                    break;

                case TouchActionType.Released:
                    Position = new SKPoint(0,0);
                    canvasView.InvalidateSurface();
                    break;
            }
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;
            
            //access view model public variables
            //should be done via binding but skiasharp does not support binding
            var vm = BindingContext as ParametersModel;

            canvas.Clear(SKColors.White);

            float width = e.Info.Width;
            float height = e.Info.Height;
            centerX = width * 0.5f;
            centerY = height * 0.5f;

            //copyright
            //canvas.DrawText("BRUCO by Alice Frigeni 2020", width * 0.1f, height * 0.95f, textPaint);

            //draw movement circle
            radius = width * 0.45f;
            canvas.Translate(centerX, centerY);
            canvas.DrawCircle(0, 0, radius, blackStroke);
            for (int i=0; i<360; i+=45)
            {
                canvas.DrawLine(0, radius-10, 0, radius+10, blackStroke);
                canvas.RotateDegrees(45);
            }

            //draw finger tracker
            canvas.DrawCircle(Position, 25, redFill);

            //calculate joystick coordinates
            int JoyX = 0, JoyY = 0;
            ConvertToJoystick(Position, ref JoyX, ref JoyY);

            //assign joystick values to view model for bluetooth communication
            vm.JoyX = JoyX;
            vm.JoyY = JoyY;

            //canvas.DrawText("JoyX = " + JoyX.ToString(), 0, -100, textPaint);
            //canvas.DrawText("JoyY = " + JoyY.ToString(), 20, -100, textPaint);


        }

        SKPoint ConvertToPixel(Point pt)
        {
            return new SKPoint((float)(canvasView.CanvasSize.Width * pt.X / canvasView.Width) - centerX,
                               (float)(canvasView.CanvasSize.Height * pt.Y / canvasView.Height) - centerY);
        }

        void ConvertToJoystick(SKPoint pt, ref int X, ref int Y)
        {
            X = (int)(100.0f * pt.X / radius);
            Y = (int)(100.0f * -pt.Y / radius);
            //clamp return values
            if (X > 100) { X = 100; }
            if (X < -100) { X = -100; }
            if (Y > 100) { Y = 100; }
            if (Y < -100) { Y = -100; }
        }

    }
}
