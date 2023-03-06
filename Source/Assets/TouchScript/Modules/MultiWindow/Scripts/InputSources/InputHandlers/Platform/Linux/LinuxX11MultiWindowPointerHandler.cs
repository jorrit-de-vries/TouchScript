using System;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    class LinuxX11MultiWindowPointerHandler : LinuxMultiWindowPointHandler
    {
        /// <summary>
        /// Should the primary pointer also dispatch a mouse pointer.
        /// </summary>
        public bool MouseInPointer
        {
            get { return mouseInPointer; }
            set
            {
                //WindowsUtils.EnableMouseInPointer(value);
                
                mouseInPointer = value;
                if (mouseInPointer)
                {
                    if (mousePointer == null) mousePointer = internalAddMousePointer(Vector3.zero);
                }
                else
                {
                    if (mousePointer != null)
                    {
                        if ((mousePointer.Buttons & Pointer.PointerButtonState.AnyButtonPressed) != 0)
                        {
                            mousePointer.Buttons = PointerUtils.UpPressedButtons(mousePointer.Buttons);
                            releasePointer(mousePointer);
                        }
                        removePointer(mousePointer);
                    }
                }
            }
        }
        
        private bool mouseInPointer = true;
        
        public LinuxX11MultiWindowPointerHandler(IntPtr window, PointerDelegate addPointer,
            PointerDelegate updatePointer,
            PointerDelegate pressPointer, PointerDelegate releasePointer, PointerDelegate removePointer,
            PointerDelegate cancelPointer)
            : base(window, addPointer, updatePointer, pressPointer, releasePointer, removePointer, cancelPointer)
        {
            
        }
        
        /// <inheritdoc />
        public override void Dispose()
        {
            if (mousePointer != null)
            {
                cancelPointer(mousePointer);
                mousePointer = null;
            }
            if (penPointer != null)
            {
                cancelPointer(penPointer);
                penPointer = null;
            }

            //WindowsUtils.EnableMouseInPointer(false);

            base.Dispose();
        }
        
        /// <inheritdoc />
        public override bool UpdateInput()
        {
            // base.UpdateInput();
            // Proces the X11 
            
            return true;
        }

        /// <inheritdoc />
        public override bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            if (pointer.Equals(mousePointer))
            {
                cancelPointer(mousePointer);
                if (shouldReturn) mousePointer = internalReturnMousePointer(mousePointer);
                else mousePointer = internalAddMousePointer(pointer.Position); // can't totally cancel mouse pointer
                return true;
            }
            if (pointer.Equals(penPointer))
            {
                cancelPointer(penPointer);
                if (shouldReturn) penPointer = internalReturnPenPointer(penPointer);
                return true;
            }
            return base.CancelPointer(pointer, shouldReturn);
        }

        /// <inheritdoc />
        public override void INTERNAL_DiscardPointer(Pointer pointer)
        {
            if (pointer is MousePointer) mousePool.Release(pointer as MousePointer);
            else if (pointer is PenPointer) penPool.Release(pointer as PenPointer);
            else base.INTERNAL_DiscardPointer(pointer);
        }
    }
}