using System.Runtime.InteropServices;
using JWCEssentials.net;

namespace CrystalCatalystLibrary;
public partial class DataInterchange
{
    public class Imports
    {
        // P_INSTANCE DataInterchange DataInterchange_Create()
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  DataInterchange_Create();

        // void DataInterchange_Free(P_INSTANCE DataInterchange drag)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  DataInterchange_Free(IntPtr drag);

        // P_INSTANCE DataInterchange::Node DataInterchange_FormatAdd(P_INSTANCE DataInterchange drop, utf8_string_struct format)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  DataInterchange_FormatAdd(IntPtr drop, ref utf8_string_struct format);

        // bool DataInterchange_FormatExists(P_INSTANCE DataInterchange data, utf8_string_struct format)
        [DllImport("CrystalCatalystLibrary")]
        public static extern bool  DataInterchange_FormatExists(IntPtr data, ref utf8_string_struct format);

        // P_INSTANCE DataInterchange::Node DataInterchange_FormatEnum(P_INSTANCE DataInterchange drop)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  DataInterchange_FormatEnum(IntPtr drop);

        // P_INSTANCE DataInterchange::Node DataInterchange_FormatEnumNext(P_INSTANCE DataInterchange::Node node)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  DataInterchange_FormatEnumNext(IntPtr node);

        // void DataInterchange_FormatEnumText(P_INSTANCE DataInterchange::Node node, P_OUT utf8_string_struct text)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  DataInterchange_FormatEnumText(IntPtr node, ref utf8_string_struct text);

        // P_INSTANCE DataInterchange::Node DataInterchange_ItemsFormatRemove(P_INSTANCE DataInterchange drop, P_INSTANCE DataInterchange::Node node)
        [DllImport("CrystalCatalystLibrary")]
        public static extern IntPtr  DataInterchange_ItemsFormatRemove(IntPtr drop, IntPtr node);

        // void DataInterchange_Select(P_INSTANCE DataInterchange data, utf8_string_struct format)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  DataInterchange_Select(IntPtr data, ref utf8_string_struct format);

        // void DataInterchange_SelectionReveal(P_INSTANCE DataInterchange drag, P_OUT utf8_string_struct format, P_OUT P_INSTANCE void data, P_OUT size_t size)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  DataInterchange_SelectionReveal(IntPtr drag, ref utf8_string_struct format, IntPtr data, IntPtr size);

        // void DataInterchange_SelectionSet(P_INSTANCE DataInterchange drag, utf8_string_struct format, P_INSTANCE void data, size_t size)
        [DllImport("CrystalCatalystLibrary")]
        public static extern void  DataInterchange_SelectionSet(IntPtr drag, ref utf8_string_struct format, IntPtr data, IntPtr size);

    }
    public IntPtr Handle;
    public DataInterchange(IntPtr Handle)
    {
        this.Handle= Handle;
    }
    public static DataInterchange Create()
    {
        return new DataInterchange(Imports.DataInterchange_Create());
    }
    public void Free()
    {
        Imports.DataInterchange_Free(Handle);
    }
    public IntPtr FormatAdd(string  format)
    {
        utf8_string_struct param_format = format;
        return (IntPtr) Imports.DataInterchange_FormatAdd(Handle,  ref param_format);
    }
    public bool FormatExists(string  format)
    {
        utf8_string_struct param_format = format;
        return (bool) Imports.DataInterchange_FormatExists(Handle,  ref param_format);
    }
    public IntPtr FormatEnum()
    {
        return (IntPtr) Imports.DataInterchange_FormatEnum(Handle);
    }
    public static IntPtr FormatEnumNext(IntPtr  node)
    {
        return (IntPtr) Imports.DataInterchange_FormatEnumNext((IntPtr)node);
    }
    public static void FormatEnumText(IntPtr  node, string  text)
    {
        utf8_string_struct param_text = text;
        Imports.DataInterchange_FormatEnumText((IntPtr)node,  ref param_text);
    }
    public IntPtr ItemsFormatRemove(IntPtr  node)
    {
        return (IntPtr) Imports.DataInterchange_ItemsFormatRemove(Handle, (IntPtr)node);
    }
    public void Select(string  format)
    {
        utf8_string_struct param_format = format;
        Imports.DataInterchange_Select(Handle,  ref param_format);
    }
    public void SelectionReveal(string  format, IntPtr  data, IntPtr  size)
    {
        utf8_string_struct param_format = format;
        Imports.DataInterchange_SelectionReveal(Handle,  ref param_format, (IntPtr)data, (IntPtr)size);
    }
    public void SelectionSet(string  format, IntPtr  data, IntPtr  size)
    {
        utf8_string_struct param_format = format;
        Imports.DataInterchange_SelectionSet(Handle,  ref param_format, (IntPtr)data, (IntPtr)size);
    }
}
