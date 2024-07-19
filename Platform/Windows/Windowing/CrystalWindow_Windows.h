// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_WINDOWS_CRYSTALWINDOW_H
#define CRYSTALCATALYST_WINDOWS_CRYSTALWINDOW_H

#include "../../../CrystalCatalystLibrary.h"
#include "../Platform.h"

class CrystalWindow_Windows : public CrystalWindow, public IDropTarget , public IDropSource
{
private:
    LONG m_cRef;

public:
    CrystalWindow_Windows();
    HWND hwnd;
    HINSTANCE hInstance;
    HGLRC gl_context;

    P_INSTANCE(WindowHandle) myHandle;

    virtual void PresentImage(utf8_string_const pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height);
    virtual void QueueRedraw();

    virtual void MouseCapture();
    virtual void MouseRelease();
    virtual void GL_Init();
    virtual void Show(bool restore);

    virtual void RegisterDragTarget();
    virtual void DragStart(P_INSTANCE(DragDropData) data, int32_t x, int32_t y);


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

#endif //CRYSTALCATALYST_WINDOWS_CRYSTALWINDOW_H
