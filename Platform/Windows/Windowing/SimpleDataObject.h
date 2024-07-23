#ifndef SIMPLEDATAOBJECT_H
#define SIMPLEDATAOBJECT_H

#include <objidl.h>
#include <cstdint>

#define mod_header() "DragDrop_Windows:"

//Write Format
class FormatEtcEnumerator : public IEnumFORMATETC {
public:
    FormatEtcEnumerator(FORMATETC* fmt, int32_t count);
    ~FormatEtcEnumerator();
    STDMETHOD(QueryInterface)(REFIID riid, P_OUT(P_INSTANCE(void))  ppv);
    STDMETHOD_(ULONG, AddRef)();
    STDMETHOD_(ULONG, Release)();
    STDMETHOD(Next)(ULONG celt, FORMATETC* rgelt, ULONG* pceltFetched);
    STDMETHOD(Skip)(ULONG celt);
    STDMETHOD(Reset)();
    STDMETHOD(Clone)(IEnumFORMATETC** ppEnum);

private:
    LONG m_refs;
    int32_t m_count;
    int32_t m_index;
    FORMATETC* m_fmt;
};

// Write Data
class SimpleDataObject : public IDataObject {
public:
    P_INSTANCE(DataInterchange) dataInterchange;
    SimpleDataObject(P_INSTANCE(DataInterchange) dataInterchange, FORMATETC* fmt, int32_t count, P_INSTANCE(WindowHandle) handle);
    ~SimpleDataObject();
    STDMETHOD(QueryInterface)(REFIID riid, P_OUT(P_INSTANCE(void))  ppv);
    STDMETHOD_(ULONG, AddRef)();
    STDMETHOD_(ULONG, Release)();
    STDMETHOD(GetData)(FORMATETC* pFormatEtc, STGMEDIUM* pMedium);
    STDMETHOD(GetDataHere)(FORMATETC* pFormatEtc, STGMEDIUM* pMedium);
    STDMETHOD(QueryGetData)(FORMATETC* pFormatEtc);
    STDMETHOD(GetCanonicalFormatEtc)(FORMATETC* pFormatEtc, FORMATETC* pFormatEtcOut);
    STDMETHOD(SetData)(FORMATETC* pFormatEtc, STGMEDIUM* pMedium, BOOL fRelease);
    STDMETHOD(EnumFormatEtc)(DWORD dwDirection, IEnumFORMATETC** ppEnumFormatEtc);
    STDMETHOD(DAdvise)(FORMATETC* pFormatEtc, DWORD advf, IAdviseSink* pAdvSink, DWORD* pdwConnection);
    STDMETHOD(DUnadvise)(DWORD dwConnection);
    STDMETHOD(EnumDAdvise)(IEnumSTATDATA** ppEnumAdvise);

private:
    LONG m_refs;
    int32_t m_count;
    FORMATETC* m_fmt;
    P_INSTANCE(WindowHandle) m_handle;
};

HRESULT CreateDataObject(P_INSTANCE(DataInterchange) dta, FORMATETC* fmt, int32_t count, P_INSTANCE(WindowHandle) handle, IDataObject** ppDataObject);

// Read Format
HRESULT DataInterchange_ReadFormats(P_INSTANCE(DataInterchange) data, IDataObject * pDataObject);

void DataInterchange_CreateContext(P_INSTANCE(DataInterchange) data);
HGLOBAL DataInterchange_MakeHGLOBAl(P_INSTANCE(DataInterchange) dataInterchange, utf8_string_const format, UINT * cfFormat);

#endif //SIMPLEDATAOBJECT_H
