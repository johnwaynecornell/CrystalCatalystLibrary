// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include <cmath>
#include <iostream>
#include <fstream>
#include <string>
#include <filesystem>
namespace fs = std::filesystem;

#include "../CrystalCatalystLibrary.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "test_client:"

utf8_string_const format_prec[] = { "text/file-uri", "text/html", "text/plain", nullptr };
//utf8_string_const format_prec[] = { "text/file-uri", "text/plain", nullptr };

struct Pix {
    double R;
    double A;
    double B;
    double G;
};

utf8_string_const Pix_format = "RABG:float64";

int32_t window_width =0;
int32_t window_height =0;

double frac(double v) {
    return v - trunc(v);
}

void on_draw(P_INSTANCE(WindowHandle) window_handle) {
    std::cerr << mod_header() << "Paint event" << std::endl;
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


// struct Pix {
//     uint8_t B;
//     uint8_t G;
//     uint8_t R;
//     uint8_t A;
// };
//
// utf8_string_const Pix_format = "BGRA:int8";
//
// int32_t window_width =0;
// int32_t window_height =0;
//
// void on_draw(P_INSTANCE(WindowHandle) window_handle) {
//     std::cerr << mod_header() << "Paint event" << std::endl;
//     if (window_width == 0 || window_height == 0) return;
//
//     size_t sqr = window_width*window_height;
//     Pix *buffer = new Pix[sqr];
//     Pix *pix = buffer;
//
//     for (int32_t y=0; y < window_height; y++) {
//         for (int32_t x =0; x < window_width; x++) {
//             if (y < 30 && x < 90) {
//                 (*pix).R = 0;
//                 (*pix).G = 0;
//                 (*pix).B = 0;
//                 (*pix).A = 0xFF;
//
//
//                 if (x<30) (*pix).R = 0xFF;
//                 else if (x<60) (*pix).G = 0xFF;
//                 else (*pix).B = 0xFF;
//
//             } else {
//                 (*pix).R = static_cast<uint8_t>(x + y);
//                 (*pix).G = static_cast<uint8_t>(x * y);
//                 (*pix).B = static_cast<uint8_t>(y);
//                 (*pix).A = 0xFF;
//             }
//             pix++;
//         }
//     }
//
//     CrystalWindow_PresentImage(window_handle, Pix_format, buffer, sqr * sizeof(Pix), window_width, window_height);
//
//     delete []buffer;
// }

void on_key_down(P_INSTANCE(WindowHandle) window_handle, int32_t keycode) {
    std::cerr << mod_header() << "Key down event: " << keycode << std::endl;

    if (keycode == 'c' || keycode == 'C') {
        std::cerr << mod_header() << "Clipboard copy" << std::endl;

        P_INSTANCE(DataInterchange)data = DataInterchange_Create();
        DataInterchange_FormatAdd(data, "text/file-uri");
        Clipboard_Copy(window_handle, data);

    } else if (keycode == 'p' || keycode == 'P') {
        DataInterchange *data = Clipboard_Paste();

        if (data == nullptr) {
            std::cerr << mod_header() << "Clipboard error" << std::endl;
            return;
        }

        std::cerr << mod_header() << "Clipboard paste" << std::endl;
        // Process the pasted data
        utf8_string_const type;
        P_INSTANCE(void)data_ptr;
        size_t size;

        int32_t i = 0;
        while (format_prec[i] != nullptr) i++;

        utf8_string_const format = nullptr;

        for (P_INSTANCE(DragDropData::Node)node = DataInterchange_FormatEnum(data);
             i != 0 && node != nullptr; node = DataInterchange_FormatEnum_Next(node)) {
            utf8_string_const drop_format;
            DataInterchange_FormatEnum_Text(node, &drop_format);

            for (int32_t i2 = 0; i2 < i; i2++) {
                if (strcmp(format_prec[i2], drop_format) == 0) {
                    i = i2;
                    format = format_prec[i];
                    break;
                }
            }
        }

        DataInterchange_Select(data, format);
        DataInterchange_Selection_Reveal(data, &type, &data_ptr, &size);

        std::cerr << mod_header() << "Pasted data of type: " << type << ", size: " << size << std::endl;

        if (StartingWith("text/", type)) {
            std::cerr << mod_header() << "Text data:" << std::endl;
            std::cerr << mod_header() << (utf8_string_const) data_ptr << std::endl;

            /*
            // Print32_t the first 100 characters of the data for debugging
            std::string text_data(static_cast<utf8_string >(data_ptr), size);
            std::cerr << mod_header() << "Text data: " << text_data.substr(0, 100) << std::endl;

            // Check the first few bytes of the data for debugging
            std::cerr << mod_header() << "First few bytes of data: ";
            for (size_t i = 0; i < std::min(size, size_t(10)); ++i) {
                std::cerr << mod_header() << std::hex << static_cast<int>(static_cast<P_ELEMENTS(uint8_t) >(data_ptr)[i]) << " ";
            }
            std::cerr << mod_header() << std::dec << std::endl;
            */
        } else {
            std::cerr << mod_header() << "Non-text data of size " << size << std::endl;

            // Optionally, handle binary data
        }
    }
}

void on_key_up(P_INSTANCE(WindowHandle) window_handle, int32_t keycode) {
    std::cerr << mod_header() << "Key up event: " << keycode << std::endl;
}

void on_mouse_move(P_INSTANCE(WindowHandle) window_handle, int32_t x, int32_t y) {
    //std::cerr << mod_header() << "Mouse move event: (" << x << ", " << y << ")" << std::endl;
}

void on_mouse_down(P_INSTANCE(WindowHandle) window_handle, int32_t button, int32_t x, int32_t y) {
    std::cerr << mod_header() << "Mouse down event: button " << button << " at (" << x << ", " << y << ")" << std::endl;

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
    std::cerr << mod_header() << "Resize event: (" << width << ", " << height << ")" << std::endl;
    window_width = width;
    window_height = height;

    //CrystalWindow_QueueRedraw(window_handle);
}

void on_close(P_INSTANCE(WindowHandle) window_handle) {
    std::cerr << mod_header() << "Close event" << std::endl;
    CrystalWindow_ApplicationRelease(window_handle);
    Application_WindowRemove(window_handle);
}

void on_focus_in(P_INSTANCE(WindowHandle) window_handle) {
    std::cerr << mod_header() << "Focus in event" << std::endl;
}

void on_focus_out(P_INSTANCE(WindowHandle) window_handle) {
    std::cerr << mod_header() << "Focus out event" << std::endl;
}

void on_drag_receive_start(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data) {
    std::cerr << mod_header() << "Drag start event" << std::endl;
    // Optionally: Prepare data to be dragged
    // Example: Set visual feedback for the drag action
}

void on_drag_receive_enter(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data) {
    std::cerr << mod_header() << "Drag enter event" << std::endl;
    // Optionally: Highlight drop target area
    // Example: Provide feedback about the type of data being dragged

    data->status.accept = true;
}

void on_drag_receive_motion(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, int32_t x, int32_t y, uint32_t key_state) {
    std::cerr << mod_header() << "Drag motion event" << std::endl;
    // Optionally: Update visual feedback as the data is dragged over the window
    // Example: Check if the data can be accepted at the current location

    char DragString[1024];
    DragActions_String(data->action_selections, DragString, 1024);
    //
    // std::cerr << mod_header() << DragString << std::endl;

    data->status.action = (DragActions) (data->action_selections & DRAG_OPERATION_COPY);
}

void on_drag_receive_leave(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data) {
    std::cerr << mod_header() << "Drag leave event" << std::endl;
    // Optionally: Remove highlight from drop target area
    // Example: Clean up any temporary states related to the drag action
}

void on_drag_receive_select(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, P_OUT(utf8_string_const) format) {
    int32_t i = 0;
    while (format_prec[i] != nullptr) i++;

    *format = nullptr;

    for (P_INSTANCE(DragDropData::Node) node = DataInterchange_FormatEnum(data); i != 0 && node != nullptr; node = DataInterchange_FormatEnum_Next(node)) {
        utf8_string_const drop_format;
        DataInterchange_FormatEnum_Text(node, &drop_format);

        for (int32_t i2 = 0; i2 < i; i2++) {
            if (strcmp(format_prec[i2], drop_format) == 0) {
                i = i2;
                *format = format_prec[i];
                break;
            }
        }
    }
}

void on_drag_provide_chosen(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, utf8_string_const format) {

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

        DataInterchange_Selection_Set(data, format, (utf8_string ) paths.c_str(), paths.length());
    }
}

void on_drag_provide_status(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData) data) {

}

void on_drag_provide_finished(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData) data, bool success)
{
    DataInterchange_Free(data);
}

void on_clipboard_provide_chosen(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data, utf8_string_const format)
{
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

        DataInterchange_Selection_Set(data, format, (utf8_string ) paths.c_str(), paths.length());
    }

}


void on_drag_receive_drop(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data) {
    if (data == nullptr) {
        std::cerr << mod_header() << "Drag and drop error" << std::endl;
        return;
    }

    std::cerr << mod_header() << "Drag drop event" << std::endl;
    // Process the dropped data
    utf8_string_const type;
    P_INSTANCE(void) data_ptr;
    size_t size;

    DataInterchange_Selection_Reveal(data, &type, &data_ptr, &size);

    std::cerr << mod_header() << "Dropped data of type: " << type << ", size: " << size << std::endl;

    if (StartingWith("text/", type)) {
        std::cerr << mod_header() << "Text data:" << std::endl;
        std::cerr << mod_header() << (utf8_string_const ) data_ptr << std::endl;

        /*
        // Print32_t the first 100 characters of the data for debugging
        std::string text_data(static_cast<utf8_string >(data_ptr), size);
        std::cerr << mod_header() << "Text data: " << text_data.substr(0, 100) << std::endl;

        // Check the first few bytes of the data for debugging
        std::cerr << mod_header() << "First few bytes of data: ";
        for (size_t i = 0; i < std::min(size, size_t(10)); ++i) {
            std::cerr << mod_header() << std::hex << static_cast<int>(static_cast<P_ELEMENTS(uint8_t) >(data_ptr)[i]) << " ";
        }
        std::cerr << mod_header() << std::dec << std::endl;
        */
    } else {
        std::cerr << mod_header() << "Non-text data of size " << size << std::endl;

        // Optionally, handle binary data
    }

    // Optionally: Clean up after drop action
}


void on_idle(P_INSTANCE(WindowHandle) window_handle) {
    //std::cerr << mod_header() << "Idle event" << std::endl;
}

int32_t main(int32_t argc, P_ELEMENTS(utf8_string)  argv) {
    std::cerr << mod_header() << "Initializing application..." << std::endl;
    Application_Init(argc, argv);

    std::string l;
    while (!CrystalCatalyst_Fonts_Has_MSCoreFonts(nullptr)) {
        //usleep(100000);
        std::cout << "Press Enter to continue..." << std::endl;
        std::getline(std::cin, l);
    }

    std::cerr << mod_header() << "Creating window..." << std::endl;
    P_INSTANCE(WindowHandle) window_handle = CrystalWindow_Create(800, 600, "Test Window");

    CrystalWindow_ApplicationRetain(window_handle);

    CrystalWindow_RegisterDragTarget(window_handle);

    std::cerr << mod_header() << "Setting message handlers..." << std::endl;
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_draw", (P_INSTANCE(void))on_draw);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_key_down", (P_INSTANCE(void))on_key_down);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_key_up", (P_INSTANCE(void))on_key_up);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_mouse_move", (P_INSTANCE(void))on_mouse_move);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_mouse_down", (P_INSTANCE(void))on_mouse_down);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_mouse_up", (P_INSTANCE(void))on_mouse_up);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_resize", (P_INSTANCE(void))on_resize);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_close", (P_INSTANCE(void))on_close);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_focus_in", (P_INSTANCE(void))on_focus_in);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_focus_out", (P_INSTANCE(void))on_focus_out);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_receive_start", (P_INSTANCE(void))on_drag_receive_start);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_receive_enter", (P_INSTANCE(void))on_drag_receive_enter);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_receive_motion", (P_INSTANCE(void))on_drag_receive_motion);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_receive_leave", (P_INSTANCE(void))on_drag_receive_leave);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_receive_select", (P_INSTANCE(void))on_drag_receive_select);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_receive_drop", (P_INSTANCE(void))on_drag_receive_drop);

    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_provide_chosen", (P_INSTANCE(void))on_drag_provide_chosen);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_provide_status", (P_INSTANCE(void))on_drag_provide_status);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_drag_provide_finished", (P_INSTANCE(void))on_drag_provide_finished);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_clipboard_provide_chosen", (P_INSTANCE(void))on_clipboard_provide_chosen);
    CrystalWindow_SetMessaqgeHandler(window_handle, "on_idle", (P_INSTANCE(void))on_idle);

    std::cerr << mod_header() << "Starting application..." << std::endl;

    CrystalWindow_Show(window_handle, true);

    Application_Run();

    return 0;
}
