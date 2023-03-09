/*
@author Jorrit de Vries (jorrit@ijsfontein.nl)
*/
#pragma once

#include <map>
#include <vector>
#include <X11/Xlib.h>
#include <X11/Xatom.h>

#include "X11TouchMultiWindowCommon.h"

class PointerHandler;
typedef std::map<Window, PointerHandler*> PointerHandlerMap;
typedef PointerHandlerMap::iterator PointerHandlerMapIterator;
typedef PointerHandlerMap::const_iterator ConstPointerHandlerMapIterator;

class EXPORT_API PointerSystem
{
private:
    Display* mDisplay;
    int mOpcode;
    MessageCallback mMessageCallback;
    PointerHandlerMap mPointerHandlers;

public:
    PointerSystem(MessageCallback messageCallback);
    ~PointerSystem();

    Result initialize();

    Result createHandler(Window window, PointerCallback pointerCallback, void** handle);
    PointerHandler* getHandler(Window window) const;
    Result destroyHandler(PointerHandler* handler);

    Result processEventQueue();

    Result getWindowsOfProcess(unsigned long  pid, Window** windows, uint* numWindows);
    Result freeWindowsOfProcess(Window* windows);
private:
    void getWindowsOfProcess(Window window, unsigned long pid, Atom atomPID, std::vector<Window>& windows);
};