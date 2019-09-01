﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Input;
using H.NET.Utilities;

namespace HomeCenter.NET.Utilities.HookModules
{
    public class ScreenshotModule : RectangleModule
    {
        #region Events

        public event EventHandler<Image> NewImage;

        private void OnNewImage(Image value)
        {
            NewImage?.Invoke(this, value);
        }

        #endregion

        #region Constructors

        public ScreenshotModule(List<Key> keys, List<ModifierKeys> modifiers) : base(keys, modifiers)
        {
            NewRectangle += async (obj, rectangle) =>
            {
                var startPoint = Screenshoter.GetVirtualDisplayStartPoint();
                rectangle.X -= startPoint.X;
                rectangle.Y -= startPoint.Y;

                using (var image = await Screenshoter.ShotVirtualDisplayRectangleAsync(rectangle))
                {
                    OnNewImage(image);
                }
            };
        }

        #endregion
    }
}
