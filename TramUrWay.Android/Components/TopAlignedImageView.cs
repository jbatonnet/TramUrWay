using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace TramUrWay.Android
{
    [Register("net.thedju.TramUrWay.TopAlignedImageView")]
    public class TopAlignedImageView : ImageView
    {
        public TopAlignedImageView(Context context) : base(context)
        {
            Setup();
        }
        public TopAlignedImageView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Setup();
        }
        public TopAlignedImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            Setup();
        }

        private void Setup()
        {
            SetScaleType(ScaleType.Matrix);
        }

        protected override bool SetFrame(int frameLeft, int frameTop, int frameRight, int frameBottom)
        {
            float frameWidth = frameRight - frameLeft;
            float frameHeight = frameBottom - frameTop;

            float originalImageWidth = Drawable.IntrinsicWidth;
            float originalImageHeight = Drawable.IntrinsicHeight;

            float usedScaleFactor = 1;

            if ((frameWidth > originalImageWidth) || (frameHeight > originalImageHeight))
            {
                // If frame is bigger than image
                // => Crop it, keep aspect ratio and position it at the bottom and center horizontally

                float fitHorizontallyScaleFactor = frameWidth / originalImageWidth;
                float fitVerticallyScaleFactor = frameHeight / originalImageHeight;

                usedScaleFactor = Math.Max(fitHorizontallyScaleFactor, fitVerticallyScaleFactor);
            }
            else
            {
                float fitHorizontallyScaleFactor = frameWidth / originalImageWidth;
                float fitVerticallyScaleFactor = frameHeight / originalImageHeight;

                usedScaleFactor = Math.Max(fitHorizontallyScaleFactor, fitVerticallyScaleFactor);
            }

            float newImageWidth = originalImageWidth * usedScaleFactor;
            float newImageHeight = originalImageHeight * usedScaleFactor;

            Matrix matrix = ImageMatrix;
            matrix.SetScale(usedScaleFactor, usedScaleFactor, 0, 0); // Replaces the old matrix completly
                                                                     //comment matrix.postTranslate if you want crop from TOP
            matrix.PostTranslate((frameWidth - newImageWidth) / 2, 0);// frameHeight - newImageHeight);
            ImageMatrix = matrix;

            return base.SetFrame(frameLeft, frameTop, frameRight, frameBottom);
        }
    }
}