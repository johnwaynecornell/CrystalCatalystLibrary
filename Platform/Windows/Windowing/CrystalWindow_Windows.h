// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_WINDOWS_CRYSTALWINDOW_H
#define CRYSTALCATALYST_WINDOWS_CRYSTALWINDOW_H

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"
#include "../Platform.h"

using namespace JWCEssentials;

namespace NewAge {

class CrystalWindow_Windows : public CrystalWindow, public IDropTarget , public IDropSource
{
private:
    LONG m_cRef;

public:
    CrystalWindow_Windows();
    ~CrystalWindow_Windows() override;
    HWND hwnd;
    HINSTANCE hInstance;
    HGLRC gl_context;

    P_INSTANCE(WindowHandle) myHandle;
    P_INSTANCE(DragDropData) current_drag_data = nullptr;

    HICON current_hIcon = nullptr;
    HCURSOR current_hCursor = nullptr;
    HDC paint_hdc = nullptr;
    bool owns_cursor = false;
    bool mouse_tracked = false;

    virtual void PresentImage(utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height);
    virtual void QueueRedraw();

    virtual void MouseCapture();
    virtual void MouseRelease();
    virtual bool GLInitAdvanced(const GLOptions& options);
    virtual void GLGetVersion(int32_t& major, int32_t& minor);
    virtual void GLMakeCurrent();
    virtual void GLPresent();
    virtual void* GLGetProcAddress(const char* name);
    virtual void Show(bool restore);

    void Close() override;
    void PostClose() override;

    void SetSize(int32_t width, int32_t height) override;
    void GetSize(int32_t& width, int32_t& height) override;
    void SetLocation(int32_t x, int32_t y) override;
    void GetLocation(int32_t& x, int32_t& y) override;

    void SetCursor(utf8_string_struct pixformat, P_ELEMENTS(void) pixdata, size_t pixdata_length, int32_t width, int32_t height, int32_t hot_x, int32_t hot_y) override;
    void SetStandardCursor(CrystalCursor cursor_enum) override;
    void SetIcon(utf8_string_struct pixformat, P_ELEMENTS(void) pixdata, size_t pixdata_length, int32_t width, int32_t height) override;
    void SetTitle(utf8_string_struct title) override;
    void GetTitle(P_OUT(utf8_string_struct) title) override;

    virtual void RegisterDragTarget();
    virtual void DragStart(P_INSTANCE(DragDropData) data, int32_t x, int32_t y);

    virtual void Activate();

    /* INTERNAL */
    HRESULT __stdcall QueryInterface(REFIID iid, P_ELEMENTS(void) * ppvObject);
    ULONG __stdcall AddRef();
    ULONG __stdcall Release();

    /* IDropTarget */
    HRESULT __stdcall DragEnter(IDataObject* pDataObject, DWORD grfKeyState, POINTL pt, DWORD* pdwEffect);
    HRESULT __stdcall DragOver(DWORD grfKeyState, POINTL pt, DWORD* pdwEffect);
    HRESULT __stdcall DragLeave();
    HRESULT __stdcall Drop(IDataObject* pDataObject, DWORD grfKeyState, POINTL pt, DWORD* pdwEffect);

    /* IDropSource */
    HRESULT __stdcall QueryContinueDrag(BOOL fEscapePressed, DWORD grfKeyState);
    HRESULT __stdcall GiveFeedback(DWORD dwEffect);
};
}
#endif //CRYSTALCATALYST_WINDOWS_CRYSTALWINDOW_H
