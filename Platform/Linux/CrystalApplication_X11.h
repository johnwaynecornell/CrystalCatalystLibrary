// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALAPPLICATION_X11_H
#define CRYSTALAPPLICATION_X11_H

#include <vector>
/*
 *  build dependencies on Ubuntu:
    sudo apt install libx11-dev libgl1-mesa-dev
*/
#include <X11/Xresource.h>

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

#include "Windowing/CrystalWindow_X11.h"
#include "Platform.h"

namespace NewAge {
    class CrystalApplication_X11 : public CrystalApplication {
    public:
#pragma pack(push, 1) // Save the current packing and set the new packing to 1 byte
        struct {
            Atom clipboard;
            Atom primary;
            Atom xdnd_selection;
            Atom targets;
            Atom selection_data;
            Atom utf8_string;

            struct {
                struct {
                    Atom none;
                    Atom copy;
                    Atom move;
                    Atom link;
                } action;

                struct {
                    Atom enter;
                    Atom position;
                    Atom status;
                    Atom leave;
                    Atom drop;
                    Atom finished;
                } msg;

                Atom aware;
                Atom type_list;
                Atom supported_actions;
                Atom selection;
            } xdnd;

            struct {
                Atom protocols;     // "WM_PROTOCOLS"
                Atom _delete;       // "WM_DELETE_WINDOW"
                Atom take_focus;    // "WM_TAKE_FOCUS"
            } window;

            struct {
                Atom net_wm_icon;   // "_NET_WM_ICON"
            } ewmh;

        } atoms;
#pragma pack(pop)

    private:
        inline void InitAtoms() {
            atoms.clipboard = GetAtom("CLIPBOARD");
            atoms.primary = GetAtom("PRIMARY");
            atoms.xdnd_selection = GetAtom("XdndSelection");
            atoms.targets = GetAtom("TARGETS");
            atoms.utf8_string = GetAtom("UTF8_STRING");
            // at app init
            atoms.selection_data = GetAtom("CRYSTAL_SELECTION");

            atoms.xdnd.action.none = GetAtom("XdndActionNone");
            atoms.xdnd.action.copy = GetAtom("XdndActionCopy");
            atoms.xdnd.action.move = GetAtom("XdndActionMove");
            atoms.xdnd.action.link = GetAtom("XdndActionLink");

            atoms.xdnd.msg.enter = GetAtom("XdndEnter");
            atoms.xdnd.msg.position = GetAtom("XdndPosition");
            atoms.xdnd.msg.status = GetAtom("XdndStatus");
            atoms.xdnd.msg.leave = GetAtom("XdndLeave");
            atoms.xdnd.msg.drop = GetAtom("XdndDrop");
            atoms.xdnd.msg.finished = GetAtom("XdndFinished");

            atoms.xdnd.aware = GetAtom("XdndAware");
            atoms.xdnd.type_list = GetAtom("XdndTypeList");
            atoms.xdnd.supported_actions = GetAtom("XdndSupportedActions");
            atoms.xdnd.selection = GetAtom("XdndSelection");

            // Window protocol atoms
            atoms.window.protocols   = XInternAtom(globalDisplay, "WM_PROTOCOLS",    False);
            atoms.window._delete     = XInternAtom(globalDisplay, "WM_DELETE_WINDOW",False);
            atoms.window.take_focus  = XInternAtom(globalDisplay, "WM_TAKE_FOCUS",  False);

            atoms.ewmh.net_wm_icon   = GetAtom("_NET_WM_ICON");
        }

        Atom GetAtom(const char* name) {
            Atom atom = XInternAtom(globalDisplay, name, False);
            if (atom == None) {
                std::cerr << "Failed to intern Atom: " << name << std::endl;
                // Throw an exception or handle the error as appropriate for your application
                throw std::runtime_error(std::string("Failed to intern Atom: ") + name);
            }
            return atom;
        }

    public:

        XContext windowContext = XUniqueContext();
        P_INSTANCE(Display)  globalDisplay = nullptr;  // Global display connection

        virtual void Init();

        virtual P_INSTANCE(WindowHandle) WindowCreate(int32_t width, int32_t height, utf8_string_struct title);
        virtual P_INSTANCE(WindowHandle) WindowCreate_Simple(int32_t width, int32_t height, utf8_string_struct title);

        virtual void DispatchEvent(XEvent &event);

        virtual void DispatchCycle();
        virtual bool HasMessage();
    };

    extern CrystalApplication_X11 *AppX11;
}
#endif //CRYSTALAPPLICATION_X11_H
