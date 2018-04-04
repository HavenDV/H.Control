﻿using System;
using System.Runtime.InteropServices;

namespace H.NET.Utilities
{
    public class LowLevelMouseHook : Hook
    {
        #region Events

        public event EventHandler<MouseEventExtArgs> MouseUp;
        public event EventHandler<MouseEventExtArgs> MouseDown;
        public event EventHandler<MouseEventExtArgs> MouseClick;
        public event EventHandler<MouseEventExtArgs> MouseClickExt;
        public event EventHandler<MouseEventExtArgs> MouseDoubleClick;
        public event EventHandler<MouseEventExtArgs> MouseWheel;

        #endregion

        #region Constructors

        public LowLevelMouseHook() : base("Low Level Mouse Hook", Winuser.WH_MOUSE_LL)
        {
        }

        #endregion

        #region Protected methods

        protected override void InternalCallback(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                return;
            }

            //Marshall the data from callback.
            var mouseHookStruct = (Win32.MouseLowLevelHookStruct)Marshal.PtrToStructure(lParam, typeof(Win32.MouseLowLevelHookStruct));

            //detect button clicked
            MouseButtons button = MouseButtons.None;
            short mouseDelta = 0;
            int clickCount = 0;
            bool mouseDown = false;
            bool mouseUp = false;

            switch (wParam)
            {
                case Winuser.WM_LBUTTONDOWN:
                    mouseDown = true;
                    button = MouseButtons.Left;
                    clickCount = 1;
                    break;
                case Winuser.WM_LBUTTONUP:
                    mouseUp = true;
                    button = MouseButtons.Left;
                    clickCount = 1;
                    break;
                case Winuser.WM_LBUTTONDBLCLK:
                    button = MouseButtons.Left;
                    clickCount = 2;
                    break;
                case Winuser.WM_RBUTTONDOWN:
                    mouseDown = true;
                    button = MouseButtons.Right;
                    clickCount = 1;
                    break;
                case Winuser.WM_RBUTTONUP:
                    mouseUp = true;
                    button = MouseButtons.Right;
                    clickCount = 1;
                    break;
                case Winuser.WM_RBUTTONDBLCLK:
                    button = MouseButtons.Right;
                    clickCount = 2;
                    break;
                case Winuser.WM_XBUTTONDOWN:
                case Winuser.WM_NCXBUTTONDOWN:
                    mouseDown = true;
                    button = MouseButtons.XButton1;
                    clickCount = 1;
                    break;
                case Winuser.WM_XBUTTONUP:
                case Winuser.WM_NCXBUTTONUP:
                    mouseUp = true;
                    button = MouseButtons.XButton1;
                    clickCount = 1;
                    break;
                case Winuser.WM_XBUTTONDBLCLK:
                case Winuser.WM_NCXBUTTONDBLCLK:
                    button = MouseButtons.XButton1;
                    clickCount = 2;
                    break;
                case Winuser.WM_MOUSEWHEEL:
                    //If the message is WM_MOUSEWHEEL, the high-order word of MouseData member is the wheel delta. 
                    //One wheel click is defined as WHEEL_DELTA, which is 120. 
                    //(value >> 16) & 0xffff; retrieves the high-order word from the given 32-bit value
                    mouseDelta = (short)((mouseHookStruct.MouseData >> 16) & 0xffff);

                    //TODO: X BUTTONS (I havent them so was unable to test)
                    //If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP, 
                    //or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released, 
                    //and the low-order word is reserved. This value can be one or more of the following values. 
                    //Otherwise, MouseData is not used. 
                    break;

                case Winuser.WM_MOUSEMOVE:
                    break;

                default:
                    mouseDown = true;
                    break;
            }

            //generate event 
            var e = new MouseEventExtArgs(
                                               button,
                                               clickCount,
                                               mouseHookStruct.Point.X,
                                               mouseHookStruct.Point.Y,
                                               mouseDelta);

            //Mouse up
            if (mouseUp)
            {
                MouseUp?.Invoke(null, e);
            }

            //Mouse down
            if (mouseDown)
            {
                e.SpecialButton = mouseHookStruct.MouseData > 0 ?
                    (int)Math.Log(mouseHookStruct.MouseData, 2) : 0;
                MouseDown?.Invoke(null, e);
            }

            //If someone listens to click and a click is heppened
            if (clickCount > 0)
            {
                MouseClick?.Invoke(null, e);
            }

            //If someone listens to click and a click is heppened
            if (clickCount > 0)
            {
                MouseClickExt?.Invoke(null, e);
            }

            //If someone listens to double click and a click is heppened
            if (clickCount == 2)
            {
                MouseDoubleClick?.Invoke(null, e);
            }

            //Wheel was moved
            if (mouseDelta != 0)
            {
                MouseWheel?.Invoke(null, e);
            }
            /*
            //If someone listens to move and there was a change in coordinates raise move event
            if (m_OldX != mouseHookStruct.Point.X || m_OldY != mouseHookStruct.Point.Y)
            {
                m_OldX = mouseHookStruct.Point.X;
                m_OldY = mouseHookStruct.Point.Y;
                if (s_MouseMove != null)
                {
                    s_MouseMove.Invoke(null, e);
                }

                if (s_MouseMoveExt != null)
                {
                    s_MouseMoveExt.Invoke(null, e);
                }
            }
            */
        }

        #endregion
    }
}