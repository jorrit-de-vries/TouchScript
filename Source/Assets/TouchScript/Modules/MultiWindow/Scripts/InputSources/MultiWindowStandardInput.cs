﻿#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
using System;
#endif
using TouchScript.Core;
using TouchScript.Pointers;
using TouchScript.Utils.Attributes;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// A display specific input handler. Holds a <see cref="MultiWindowMouseHandler"/> and/or a
    /// <see cref="MultiWindowPointerHandler"/>. Unity touch is not supported, for those situations
    /// <see cref="StandardInput"/> is a better fit.
    /// </summary>
    public class MultiWindowStandardInput : InputSource, IMultiWindowInputHandler
    {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
        private static readonly Version WIN8_VERSION = new Version(6, 2, 0, 0);
#endif

        public int TargetDisplay
        {
            get => targetDisplay;
            set
            {
                targetDisplay = Mathf.Clamp(value, 0, 7);
                if (mouseHandler != null) mouseHandler.TargetDisplay = value;
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
                if (pointerHandler != null) pointerHandler.TargetDisplay = value;
#endif
            }
        }
        
        /// <summary>
        /// Use emulated second mouse pointer with ALT or not.
        /// </summary>
        public bool EmulateSecondMousePointer
        {
            get => emulateSecondMousePointer;
            set
            {
                emulateSecondMousePointer = value;
                if (mouseHandler != null) mouseHandler.EmulateSecondMousePointer = value;
            }
        }

        [SerializeField, Min(0)] private int targetDisplay = 0;
        [ToggleLeft, SerializeField] private bool emulateSecondMousePointer = true;
        
#pragma warning disable CS0414

        [SerializeField, HideInInspector] private bool generalProps; // Used in the custom inspector
        [SerializeField, HideInInspector] private bool windowsProps; // Used in the custom inspector
        
#pragma warning restore CS0414
        
        private MultiWindowManagerInstance multiWindowManager;
        private MultiWindowMouseHandler mouseHandler;
#if !UNITY_EDITOR
# if UNITY_STANDALONE_WIN
        private WindowsMultiWindowPointerHandler pointerHandler;
# elif UNITY_STANDALONE_LINUX
        private LinuxMultiWindowPointerHandler pointerHandler;
# endif
#endif

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            
            multiWindowManager = MultiWindowManagerInstance.Instance;

            if (multiWindowManager.ShouldActivateDisplays)
            {
                // Activate additional display if it is not the main display
                var displays = Display.displays;
                if (targetDisplay > 0 && targetDisplay < displays.Length)
                {
                    var display = displays[targetDisplay];
                    if (!display.active)
                    {
                        // TODO Display activation settings?
                        
                        Display.displays[targetDisplay].Activate();
                        multiWindowManager.OnDisplayActivated(targetDisplay);
                    }
                }
            }

            if (!multiWindowManager.ShouldUpdateInputHandlersOnStart)
            {
                DoEnable();
            }
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            DoDisable();
            
            base.OnDisable();
        }
        
        [ContextMenu("Basic Editor")]
        private void SwitchToBasicEditor()
        {
            basicEditor = true;
        }

        public void UpdateInputHandlers()
        {
            DoDisable();
            DoEnable();
        }
        
        public override bool UpdateInput()
        {
            if (base.UpdateInput()) return true;
            
            var handled = false;
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
            if (pointerHandler != null)
            {
                handled = pointerHandler.UpdateInput();
            }
#endif
            if (mouseHandler != null)
            {
                if (handled) mouseHandler.CancelMousePointer();
                else handled = mouseHandler.UpdateInput();
            }
            
            return handled;
        }
        
        /// <inheritdoc />
        public override void UpdateResolution()
        {
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
            pointerHandler?.UpdateResolution();
#endif
            mouseHandler?.UpdateResolution();
        }
        
        /// <inheritdoc />
        public override bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            base.CancelPointer(pointer, shouldReturn);
            
            var handled = false;
            
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
            if (pointerHandler != null) handled = pointerHandler.CancelPointer(pointer, shouldReturn);
#endif
            if (mouseHandler != null && !handled) handled = mouseHandler.CancelPointer(pointer, shouldReturn);
            
            return handled;
        }
        
        /// <inheritdoc />
        protected override void updateCoordinatesRemapper(ICoordinatesRemapper remapper)
        {
            base.updateCoordinatesRemapper(remapper);
            
            if (mouseHandler != null) mouseHandler.CoordinatesRemapper = remapper;
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
            if (pointerHandler != null) pointerHandler.CoordinatesRemapper = remapper;
#endif
        }

        private void DoEnable()
        {
#if UNITY_EDITOR
            EnableMouse();
#else
# if UNITY_STANDALONE_WIN
            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version >= WIN8_VERSION)
            {
                // Windows 8+
                EnableTouch();
            }
            else
            {
                // Other windows
                EnableMouse();
            }
# elif UNITY_STANDALONE_LINUX
            // TODO Linux X11 Check?
            Debug.LogWarning("[TouchScript]: TODO Enable Touch");
# else
            EnableMouse();
# endif
#endif
            
            if (CoordinatesRemapper != null) updateCoordinatesRemapper(CoordinatesRemapper);
        }
        
        private void EnableMouse()
        {
            mouseHandler = new MultiWindowMouseHandler(addPointer, updatePointer, pressPointer, releasePointer, removePointer,
                cancelPointer);
            mouseHandler.EmulateSecondMousePointer = emulateSecondMousePointer;
            mouseHandler.TargetDisplay = TargetDisplay;
            
            Debug.Log($"[TouchScript] Initialized Unity mouse input for {TargetDisplay}.");
        }

#if !UNITY_EDITOR

# if UNITY_STANDALONE_WIN
        private void EnableTouch()
        {
            var window = multiWindowManager.GetWindowHandle(targetDisplay);
            if (window == IntPtr.Zero)
            {
                Debug.LogError($"[TouchScript] Failed to initialize Windows 8 pointer input for {TargetDisplay}.");
                return;
            }

            var windows8PointerHandler = new Windows8MultiWindowPointerHandler(window, addPointer, updatePointer, pressPointer,
                    releasePointer, removePointer, cancelPointer);
            windows8PointerHandler.MouseInPointer = true;
            windows8PointerHandler.TargetDisplay = TargetDisplay;
            pointerHandler = windows8PointerHandler;

            Debug.Log($"[TouchScript] Initialized Windows 8 pointer input for {TargetDisplay}.");
        }

# elif UNITY_STANDALONE_LINUX
        private void EnableTouch()
        {
            var display = multiWindowManager.GetDisplay();
            var window = multiWindowManager.GetWindowHandle(targetDisplay);
            if (window == IntPtr.Zero)
            {
                Debug.LogError($"[TouchScript] Failed to initialize Linux X11 pointer input for {TargetDisplay}.");
                return;
            }

            var linux11PointerHandler = new LinuxX11MultiWindowPointerHandler(display, window, addPointer, updatePointer, pressPointer,
                releasePointer, removePointer, cancelPointer);
            linux11PointerHandler.MouseInPointer = true;
            linux11PointerHandler.TargetDisplay = TargetDisplay;
            pointerHandler = linux11PointerHandler;

            Debug.Log($"[TouchScript] Initialized Linux X11 pointer input for {TargetDisplay}.");
        }
# endif
#endif

        private void DoDisable()
        {
            DisableMouse();
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
            DisableTouch();
#endif
        }
        
        private void DisableMouse()
        {
            if (mouseHandler != null)
            {
                mouseHandler.Dispose();
                mouseHandler = null;
                
                Debug.Log($"[TouchScript] Disposed Unity mouse input for {TargetDisplay}.");
            }
        }

#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
        private void DisableTouch()
        {
            if (pointerHandler != null)
            {
                pointerHandler.Dispose();
                pointerHandler = null;

                Debug.Log($"[TouchScript] Disposed Windows 8 pointer input for {TargetDisplay}.");
            }
        }
#endif
    }
}