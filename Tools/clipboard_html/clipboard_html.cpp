// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include <cmath>
#include <iostream>
#include <fstream>
#include <string>
#include <filesystem>
#include <unistd.h>
#include <X11/Xlib.h>
namespace fs = std::filesystem;

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

using namespace NewAge;

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "test_client:"

utf8_string_struct format_prec[] = { "text/file-uri", "text/html", "text/plain", nullptr };
//utf8_string_struct format_prec[] = { "text/file-uri", "text/plain", nullptr };

struct Pix {
    double R;
    double A;
    double B;
    double G;
};

utf8_string_struct Pix_format = "RABG:float64";

int32_t window_width =0;
int32_t window_height =0;
bool window_up = false;

double frac(double v) {
    return v - trunc(v);
}

void on_draw(P_INSTANCE(WindowHandle) window_handle) {
    window_up = true;

    if (window_width == 0 || window_height == 0) return;

    size_t sqr = window_width*window_height;
    Pix *buffer = new Pix[sqr];
    Pix *pix = buffer;

    for (int32_t y=0; y < window_height; y++) {
        for (int32_t x =0; x < window_width; x++) {
            if (y < 30 && x < 90) {
                (*pix).R = 0;
                (*pix).G = 0;
                (*pix).B = 0;
                (*pix).A = 1;


                if (x<30) (*pix).R = 1;
                else if (x<60) (*pix).G = 1;
                else (*pix).B = 1;

            } else {
                double xx = frac(x / (double)(256));
                double yy = frac(y / (double)(256));
                (*pix).R = (xx + yy);
                (*pix).G = (xx * yy);
                (*pix).B = (yy);
                (*pix).A = 0xFF;
            }
            pix++;
        }
    }

    CrystalWindow_PresentImage(window_handle, Pix_format, buffer, sqr * sizeof(Pix), window_width, window_height);

    delete []buffer;
}

void on_key_down(P_INSTANCE(WindowHandle) window_handle, int32_t keycode) {
}

void on_key_up(P_INSTANCE(WindowHandle) window_handle, int32_t keycode) {

}

void on_mouse_move(P_INSTANCE(WindowHandle) window_handle, int32_t x, int32_t y) {
    //std::cerr << mod_header() << "Mouse move event: (" << x << ", " << y << ")" << std::endl;
}

void on_mouse_down(P_INSTANCE(WindowHandle) window_handle, int32_t button, int32_t x, int32_t y) {

    if (button == 1) {
        P_INSTANCE(DragDropData) data = DragDropData_Create();
        DataInterchange_FormatAdd(data,"text/file-uri");

        data->action_selections = (DragActions) (DRAG_OPERATION_COPY | DRAG_OPERATION_MOVE | DRAG_OPERATION_LINK);
        //data->action_selections = (DragActions) (DRAG_OPERATION_LINK);

        CrystalWindow_DragStart(window_handle, data, x, y);
    }
}

void on_mouse_up(P_INSTANCE(WindowHandle) window_handle, int32_t button, int32_t x, int32_t y) {
    //std::cerr << mod_header() << "Mouse up event: button " << button << " at (" << x << ", " << y << ")" << std::endl;
}

void on_resize(P_INSTANCE(WindowHandle) window_handle, int32_t width, int32_t height) {
    window_width = width;
    window_height = height;

    //CrystalWindow_QueueRedraw(window_handle);
}

void on_close(P_INSTANCE(WindowHandle) window_handle) {
    CrystalWindow_ApplicationRelease(window_handle);
    Application_WindowRemove(window_handle);
}

void on_focus_in(P_INSTANCE(WindowHandle) window_handle) {

}

void on_focus_out(P_INSTANCE(WindowHandle) window_handle) {

}

void on_drag_receive_start(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data) {

    // Optionally: Prepare data to be dragged
    // Example: Set visual feedback for the drag action
}

void on_drag_receive_enter(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data) {

    // Optionally: Highlight drop target area
    // Example: Provide feedback about the type of data being dragged

    data->status.accept = true;
}

void on_drag_receive_motion(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, int32_t x, int32_t y, uint32_t key_state) {
    // Optionally: Update visual feedback as the data is dragged over the window
    // Example: Check if the data can be accepted at the current location

    utf8_string_struct DragString;

    DragString = DragDropData_DragActionsString(data->action_selections);
    //
    // std::cerr << mod_header() << DragString << std::endl;

    data->status.action = (DragActions) (data->action_selections & DRAG_OPERATION_COPY);
}

void on_drag_receive_leave(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data) {
    // Optionally: Remove highlight from drop target area
    // Example: Clean up any temporary states related to the drag action
}

utf8_string_struct on_drag_receive_select(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data)
{
    int32_t i = 0;
    while (format_prec[i] != nullptr) i++;

    for (P_INSTANCE(DragDropData::Node) node = DataInterchange_FormatEnum(data); i != 0 && node != nullptr; node = DataInterchange_FormatEnumNext(node)) {
        utf8_string_struct drop_format;
        DataInterchange_FormatEnumText(node, &drop_format);

        for (int32_t i2 = 0; i2 < i; i2++) {
            if (strcmp(format_prec[i2], drop_format) == 0) {
                i = i2;
                return format_prec[i];
            }
        }
    }
    return nullptr;
}

void on_drag_provide_chosen(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, utf8_string_struct format) {

    if (strcmp(format, "text/file-uri") == 0) {
        std::string paths = "";
        //fs::path pth;
        std::string pth;

        {
            std::ofstream file("test.txt");
            file << "This is some sample text\n";
            file.close();
        }


        pth = fs::absolute("test.txt").string();
        paths = paths + (std::string) pth + "\n";

        {
            std::ofstream file("test2.txt");
            file << "This is some more sample text\n";
            file.close();
        }

        pth = fs::absolute("test2.txt").string();
        paths += pth; paths += "\n";

        DataInterchange_SelectionSet(data, format, (void *) paths.c_str(), paths.length());
    }
}

void on_drag_provide_status(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData) data) {

}

void on_drag_provide_finished(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData) data, bool success)
{
    DataInterchange_Free(data);
}

void on_clipboard_provide_chosen(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data, utf8_string_struct format)
{
    if (strcmp(format, "text/html") == 0) {
        std::string cum,in;

        std::cout << "accepting input" << std::endl;

        std::string input="";

        std::string line;

        int c;
        while ((c = fgetc(stdin)) != EOF) {
         input += (char)c;
        }
        std::cout << "setting selection" << std::endl;

        DataInterchange_SelectionSet(data, format, (void *) input.c_str(), input.length());
        //CrystalWindow_PostClose(window_handle);
    }
}

void receive(std::string label, P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data) {
    if (data == nullptr) {
        return;
    }

    // Process the dropped data
    utf8_string_struct type;
    P_INSTANCE(void) data_ptr;
    size_t size;

    DataInterchange_SelectionReveal(data, &type, &data_ptr, &size);


    if (StartingWith("text/", type)) {
        std::cout << (const char *) data_ptr << std::endl;

    } else std::cerr << mod_header() << "Non-text data of size " << size << std::endl;

    CrystalWindow_PostClose(window_handle);

    // Optionally: Clean up after drop action
}

void on_clipboard_receive_data(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data) {
    receive("Clipboard", window_handle, data);
}

void on_drag_receive_drop(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data) {
    receive("Drag and drop", window_handle, data);
}


bool paste;

bool close_next_idle=false;

void on_data_interchange_error(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data, utf8_string_struct error)
{
    std::cerr << "Data Interchange Error: " << error << std::endl;
    CrystalWindow_PostClose(window_handle);
}


int drop_delay = 4;

void on_idle(P_INSTANCE(WindowHandle) window_handle) {
    if (close_next_idle) {
        std::cerr << "closing" << std::endl;
        CrystalWindow_PostClose(window_handle);
    }

    //if (!window_up) return;

    if (CrystalWindow_uptimeSeconds(window_handle) < .1) return;

    //if (drop_delay >0) usleep(5000);

    //if (drop_delay--==0)
    {
        std::cerr << mod_header() <<  " : paste" << CrystalWindow_uptimeSeconds(window_handle) << std::endl;

        if (paste) {
            DataInterchange *data = CrystalWindow_ClipboardPaste(window_handle);

            // Process the pasted data
            utf8_string_struct type;
            P_INSTANCE(void)data_ptr;
            size_t size;

            int32_t i = 0;
            while (format_prec[i] != nullptr) i++;

            utf8_string_struct format = nullptr;

            for (P_INSTANCE(DragDropData::Node)node = DataInterchange_FormatEnum(data);
                 i != 0 && node != nullptr; node = DataInterchange_FormatEnumNext(node)) {
                utf8_string_struct drop_format = nullptr;

                DataInterchange_FormatEnumText(node, &drop_format);

                for (int32_t i2 = 0; i2 < i; i2++) {
                    if (strcmp(format_prec[i2], drop_format) == 0) {
                        i = i2;
                        format = format_prec[i];
                        break;
                    }
                }
                    if (format != nullptr) break;
                 }
            if (format == nullptr) {
                std::cerr << mod_header() << "No supported format found" << std::endl;
                CrystalWindow_PostClose(window_handle);
            }
            else
                DataInterchange_Select(data, format);

        } else //copy
        {
            std::cout << "awaiting paste" << std::endl;
            P_INSTANCE(DataInterchange)data = DataInterchange_Create();

            data->m_handle = window_handle;
            data->provide_chosen = DataInterchange::provide_for_clipboard;
            data->selection_type = DataInterchange::E_CLIPBOARD;

            DataInterchange_FormatAdd(data, "text/html");
            CrystalWindow_ClipboardCopy(window_handle, data);

            //close_next_idle = true;
        }

        //dropped = true;
    }
}

int32_t main(int32_t argc, P_ELEMENTS(char *)  argv) {
    struct_array_struct<utf8_string_struct> args;
    args.Alloc(argc);

    for (int i=0; i < argc; i++) {
        args[i] = argv[i];
    }

    Application_Init(args);

    paste = ((std::string)  args[1]) == "paste";



    P_INSTANCE(WindowHandle) window_handle = CrystalWindow_CreateSimple(1, 1, "paste_html Window");

    CrystalWindow_ApplicationRetain(window_handle);

    CrystalWindow_SetMessageHandler(window_handle, "on_draw", (P_INSTANCE(void))on_draw);
    CrystalWindow_SetMessageHandler(window_handle, "on_key_down", (P_INSTANCE(void))on_key_down);
    CrystalWindow_SetMessageHandler(window_handle, "on_key_up", (P_INSTANCE(void))on_key_up);
    CrystalWindow_SetMessageHandler(window_handle, "on_mouse_move", (P_INSTANCE(void))on_mouse_move);
    CrystalWindow_SetMessageHandler(window_handle, "on_mouse_down", (P_INSTANCE(void))on_mouse_down);
    CrystalWindow_SetMessageHandler(window_handle, "on_mouse_up", (P_INSTANCE(void))on_mouse_up);
    CrystalWindow_SetMessageHandler(window_handle, "on_resize", (P_INSTANCE(void))on_resize);
    CrystalWindow_SetMessageHandler(window_handle, "on_close", (P_INSTANCE(void))on_close);
    CrystalWindow_SetMessageHandler(window_handle, "on_focus_in", (P_INSTANCE(void))on_focus_in);
    CrystalWindow_SetMessageHandler(window_handle, "on_focus_out", (P_INSTANCE(void))on_focus_out);
    CrystalWindow_SetMessageHandler(window_handle, "on_drag_receive_start", (P_INSTANCE(void))on_drag_receive_start);
    CrystalWindow_SetMessageHandler(window_handle, "on_drag_receive_enter", (P_INSTANCE(void))on_drag_receive_enter);
    CrystalWindow_SetMessageHandler(window_handle, "on_drag_receive_motion", (P_INSTANCE(void))on_drag_receive_motion);
    CrystalWindow_SetMessageHandler(window_handle, "on_drag_receive_leave", (P_INSTANCE(void))on_drag_receive_leave);
    CrystalWindow_SetMessageHandler(window_handle, "on_drag_receive_select", (P_INSTANCE(void))on_drag_receive_select);
    CrystalWindow_SetMessageHandler(window_handle, "on_drag_receive_drop", (P_INSTANCE(void))on_drag_receive_drop);

    CrystalWindow_SetMessageHandler(window_handle, "on_drag_provide_chosen", (P_INSTANCE(void))on_drag_provide_chosen);
    CrystalWindow_SetMessageHandler(window_handle, "on_drag_provide_status", (P_INSTANCE(void))on_drag_provide_status);
    CrystalWindow_SetMessageHandler(window_handle, "on_drag_provide_finished", (P_INSTANCE(void))on_drag_provide_finished);
    CrystalWindow_SetMessageHandler(window_handle, "on_clipboard_provide_chosen", (P_INSTANCE(void))on_clipboard_provide_chosen);
    CrystalWindow_SetMessageHandler(window_handle, "on_clipboard_receive_data", (P_INSTANCE(void))on_clipboard_receive_data);
    CrystalWindow_SetMessageHandler(window_handle, "on_data_interchange_error", (P_INSTANCE(void))on_data_interchange_error);
    CrystalWindow_SetMessageHandler(window_handle, "on_idle", (P_INSTANCE(void))on_idle);

    CrystalWindow_Show(window_handle, true);
    CrystalWindow_RegisterDragTarget(window_handle);

    Application_Run();

    return 0;
}
