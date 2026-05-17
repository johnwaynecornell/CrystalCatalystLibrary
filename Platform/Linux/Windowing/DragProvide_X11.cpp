// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalWindow_X11.h"

#include <cstring>
#include <sstream>
#include <iomanip>

#include "../CrystalApplication_X11.h"
#include "DragDrop_X11.h"
#include "DragProvide_X11.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() std::hex << std::setw(8) << std::setfill('0') << "DragProvide_X11: source=" \
<< std::hex << std::setw(8) << std::setfill('0') << (unsigned long long) source_window->window << std::dec \
<< " target=" \
<< std::hex << std::setw(8) << std::setfill('0') << (unsigned long long) target_window << std::dec << " "\

using namespace JWCEssentials;

namespace NewAge {

    DragProvide_X11::DragProvide_X11(P_INSTANCE(CrystalWindow_X11)source_window)
        : source_window(source_window), target_window(None), drag_data(nullptr), dragging(false), entered(false) {
    }

    DragProvide_X11::~DragProvide_X11() {
        if (dragging) {
            EndDrag(false);
        }
    }

    void DragProvide_X11::StartDrag(P_INSTANCE(DragDropData)  data, int32_t x, int32_t y) {
        std::cerr << mod_header() << "CrystalWindow_DragStart" << std::endl;

        Atom XA_XdndSelection = AppX11->atoms.xdnd.selection;

        XSetSelectionOwner(source_window->display, XA_XdndSelection, source_window->window, source_window->last_user_time);

        if (XGetSelectionOwner(source_window->display, XA_XdndSelection) != source_window->window) {
            std::cerr << mod_header() << "Failed to acquire selection" << std::endl;
            return; // Selection not acquired
        }

        std::cerr << mod_header() << "Selection acquired" << std::endl;

        drag_data = data;
        dragging = true;

        clear_target();

        source_window->MouseCapture();

        Atom XdndAware = AppX11->atoms.xdnd.aware;
        int64_t version = 5; // Xdnd version
        XChangeProperty(source_window->display, source_window->window, XdndAware, XA_ATOM, 32, PropModeReplace, (P_ELEMENTS(uint8_t) )&version, 1);

        UpdateMotion(x, y);

        std::cerr << mod_header() << "Drag start setup complete" << std::endl;
    }

    void DragProvide_X11::clear_target() {
        target_window = None;
        wait_for_status = false;
        if (drag_data) {
            drag_data->has_status = false;
            drag_data->status.accept = false;
            drag_data->status.action = DRAG_OPERATION_NONE;
        }
    }

    void DragProvide_X11::UpdateMotion(int32_t x, int32_t y) {
        if (!dragging) return;

        /* convert x and y to root coordinates and find the window under them */
        Window new_target = get_window_under_cursor(x, y);

        if (new_target != target_window) {
            if (entered) {
                if (target_window) {
                    send_xdnd_leave();
                    clear_target();
                }
            }
            if (new_target != None) {
                target_window = new_target;
                send_xdnd_enter();
                entered = true;
            }
        }

        if (entered) {
            if (!wait_for_status) {
                send_xdnd_position(x, y);
            }
        }
    }

    bool DragProvide_X11::handle_message(P_INSTANCE(XEvent) event) {

        if (!dragging) return false;

        switch (event->type) {
            case MotionNotify:
                UpdateMotion(event->xmotion.x, event->xmotion.y);
            return true;
            case ButtonRelease:
                EndDrag(true);
            return true;
        }

        if (handle_xdnd_status(event)) return true;
        if (handle_xdnd_finished(event)) return true;

        return false;
    }

    bool DragProvide_X11::handle_xdnd_status(P_INSTANCE(XEvent)  event) {
        if (event->type != ClientMessage) return false;
        if (event->xclient.message_type != AppX11->atoms.xdnd.msg.status) return false;

        std::cerr << mod_header() << std::hex << std::setw(8) << std::setfill('0') << "status receive l[1] = " << event->xclient.data.l[1] << std::dec << std::endl;

        bool accept = event->xclient.data.l[1] & 1;
        int64_t action = event->xclient.data.l[4];

        // Read the target rectangle
        int32_t x = (event->xclient.data.l[2] >> 16) & 0xFFFF;
        int32_t y = event->xclient.data.l[2] & 0xFFFF;
        int32_t width = (event->xclient.data.l[3] >> 16) & 0xFFFF;
        int32_t height = event->xclient.data.l[3] & 0xFFFF;

        drag_data->status.accept = accept;
        drag_data->status.action = xint64_t_to_drag_actions(action, source_window->display);

        std::cerr << mod_header() << "XdndStatus received. Accept: " << accept
                      << ", Action: " << DragDropData_DragActionsString(drag_data->status.action)
                      << ", Rect: [" << x << ", " << y << ", " << width << ", " << height << "]"
                      << std::endl;

        wait_for_status = false;
        drag_data->has_status = true;

        source_window->callbacks.on_drag_provide_status(source_window->myHandle, drag_data);
        return true;
    }

    void DragProvide_X11::drag_is_finished(bool success) {
        std::cerr << mod_header() << "drag_is_finished(success = " << (success ? "true" : "false") << ")" << std::endl;

        source_window->callbacks.on_drag_provide_finished(source_window->myHandle, drag_data, success);

        clear_target();

        dragging = false;
        drag_data = nullptr;

        source_window->MouseRelease();
    }

    bool DragProvide_X11::handle_xdnd_finished(P_INSTANCE(XEvent)  event) {
        if (event->type != ClientMessage) return false;
        if (event->xclient.message_type != AppX11->atoms.xdnd.msg.finished) return false;

        bool success = event->xclient.data.l[2] != 0;

        std::cerr << mod_header() << "XdndFinished received" << std::endl;

        drag_is_finished(success);

        return true;
    }

    void DragProvide_X11::EndDrag(bool success) {
        if (!dragging) return;

        bool fin = true;

        if (entered) {
            if (success) {
                if (drag_data->has_status) { send_xdnd_drop(); fin = false; }
            } else {
                send_xdnd_leave();
            }
        }

        if (fin) {
            drag_is_finished(false);
        }

    }

    Window window_at_coordinates(P_INSTANCE(Display) display, Window parent, int32_t x, int32_t y) {
        Window root_return, child_return;
        Window target = None;
        int32_t root_x, root_y, win_x, win_y;
        uint32_t mask_return;

        if (!XTranslateCoordinates(display, parent, parent, x, y, &root_x, &root_y, &child_return)) {
            return None;
        }

        if (child_return == None) return None;

        target = child_return;

        // Loop to find the deepest child window at the coordinates
        while (true) {
            if (!XQueryPointer(display, child_return, &root_return, &child_return, &root_x, &root_y, &win_x, &win_y, &mask_return)) {
                break;
            }

            if (child_return == None) {
                break;
            }

            target = child_return;
        }


        return target;
    }

    /* convert x and y to root coordinates and find the window under them */
    Window DragProvide_X11::get_window_under_cursor(int32_t &x, int32_t &y) {
        Window root = DefaultRootWindow(source_window->display);
        Window child_return, root_return;

        source_window->CoordsToRoot(x, y);

        Window target_window = window_at_coordinates(source_window->display, root, x, y);

        return target_window;
    }

    void DragProvide_X11::send_xdnd_enter() {
        XEvent event;
        memset(&event, 0, sizeof(event));
        event.type = ClientMessage;
        event.xclient.display = source_window->display;
        event.xclient.window = target_window;
        event.xclient.message_type = AppX11->atoms.xdnd.msg.enter;
        event.xclient.format = 32;
        event.xclient.data.l[0] = source_window->window;

        std::cerr << mod_header() << "sending XdndEnter message sent to " << target_window << " from " << source_window->window << std::endl;

        int32_t type_count = 0;
        Atom *drop_types = new Atom[type_count];

        DataImterchange_AtomArrayFromFormats(drag_data, &drop_types, &type_count);

        // Set the version in the most significant byte and indicate if more than three types are used
        event.xclient.data.l[1] = (5 << 24); // Set version 5
        if (type_count > 3) {
            event.xclient.data.l[1] |= 1; // Set the "more than three types" bit
        }

        if (type_count > 3) {
            Atom XdndTypeList = AppX11->atoms.xdnd.type_list;
            XChangeProperty(
                source_window->display,
                source_window->window,
                XdndTypeList,
                XA_ATOM,
                32,
                PropModeReplace,
                reinterpret_cast<P_ELEMENTS(uint8_t)>(drop_types),
                type_count
            );
        } else {
            for (int32_t i = 0; i < type_count; ++i) {
                event.xclient.data.l[2 + i] = drop_types[i];
            }
        }
        // Use a separate property to store the supported actions
        Atom XdndSupportedActions = AppX11->atoms.xdnd.supported_actions;

        int64_t actions = drag_actions_to_xint64_t(drag_data->action_selections, source_window->display);

        std::cerr << mod_header() << DragDropData_DragActionsString(drag_data->action_selections) << " translated as "
            << std::hex << std::setw(8) << std::setfill('0') << actions << std::dec << std::endl;

        XChangeProperty(
            source_window->display,
            source_window->window,
            XdndSupportedActions,
            XA_ATOM,
            32,
            PropModeReplace,
            reinterpret_cast<P_ELEMENTS(uint8_t)>(&actions),
            1
        );

        delete[] drop_types;
        XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
        XFlush(source_window->display);

        std::cerr << mod_header() << "XdndEnter message sent to " << target_window << " from " << source_window->window << std::endl;
    }

    void DragProvide_X11::send_xdnd_position(int32_t x, int32_t y) {
        XEvent event;
        memset(&event, 0, sizeof(event));
        event.type = ClientMessage;
        event.xclient.display = source_window->display;
        event.xclient.window = target_window;

        event.xclient.message_type = AppX11->atoms.xdnd.msg.position;
        event.xclient.format = 32;
        event.xclient.data.l[0] = (long) source_window->window;
        event.xclient.data.l[1] = source_window->last_user_time;
        event.xclient.data.l[2] = (x << 16) | y; // Pack the coordinates into l[2]
        event.xclient.data.l[3] = source_window->last_user_time;
        event.xclient.data.l[4] = drag_actions_to_xint64_t(drag_data->action_selections, source_window->display);

        XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
        XFlush(source_window->display);

        std::cerr << mod_header() << "XdndPosition message sent from " << source_window->window << ", to " << target_window << " x=" << x << " y=" << y
        << " l[0]=" << std::hex << std::setw(8) << std::setfill('0') << event.xclient.data.l[0] << std::dec
        << " l[1]=" << std::hex << std::setw(8) << std::setfill('0') << event.xclient.data.l[1] << std::dec
        << " l[3]=" << std::hex << std::setw(8) << std::setfill('0') << event.xclient.data.l[3] << std::dec
        << " l[4]=" << std::hex << std::setw(8) << std::setfill('0') << event.xclient.data.l[4] << std::dec
        << std::endl;

        drag_data->has_status = false;
        wait_for_status = true;
    }

    void DragProvide_X11::send_xdnd_leave() {
        XEvent event;
        memset(&event, 0, sizeof(event));
        event.type = ClientMessage;
        event.xclient.window = target_window;
        event.xclient.message_type = AppX11->atoms.xdnd.msg.leave;
        event.xclient.format = 32;
        event.xclient.data.l[0] = (long) source_window->window;

        XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
        XFlush(source_window->display);

        std::cerr << mod_header() << "XdndLeave message sent to " << target_window << std::endl;
    }

    void DragProvide_X11::send_xdnd_drop() {
        XEvent event;
        memset(&event, 0, sizeof(event));
        event.type = ClientMessage;
        event.xclient.display = source_window->display;
        event.xclient.window = target_window;
        event.xclient.message_type = AppX11->atoms.xdnd.msg.drop;
        event.xclient.format = 32;
        event.xclient.data.l[0] = (long) source_window->window;
        event.xclient.data.l[1] = source_window->last_user_time;

        XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
        XFlush(source_window->display);

        std::cerr << mod_header() << "XdndDrop message sent to " << target_window << std::endl;
    }

    void DragProvide_X11::send_xdnd_finished(bool success) {
        XEvent event;
        memset(&event, 0, sizeof(event));
        event.type = ClientMessage;
        event.xclient.display = source_window->display;
        event.xclient.window = target_window;
        event.xclient.message_type = AppX11->atoms.xdnd.msg.finished;
        event.xclient.format = 32;
        event.xclient.data.l[0] = (long) source_window->window;
        event.xclient.data.l[1] = success ? 1 : 0; // Success flag
        event.xclient.data.l[2] = (long) AppX11->atoms.xdnd.action.copy;

        XSendEvent(source_window->display, target_window, False, NoEventMask, &event);
        XFlush(source_window->display);

        std::cerr << mod_header() << "XdndFinished message sent to " << target_window << std::endl;
    }
}