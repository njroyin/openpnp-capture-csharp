using System.Reflection;
using System.Runtime.InteropServices;

namespace openpnp.capture.csharp;

///< an opaque pointer to the internal Context*
using CapContext = IntPtr ;    
///< a stream identifier (normally >=0, <0 for error)
using CapStream = Int32;   
///< result defined by CAPRESULT_xxx
using CapResult = UInt32;    
///< unique device ID
using CapDeviceID= UInt32 ;
///< format identifier 0 .. numFormats
using CapFormatID = UInt32;   
///< property ID (exposure, zoom, focus etc.)
using  CapPropertyID = UInt32;

 
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]  
public delegate void CapCustomLogFunc(UInt32 level, String value); 

[StructLayout(LayoutKind.Sequential)]
public struct CapFormatInfo
{
    ///< width in pixels
    public UInt32 width ;

    ///< height in pixels
    public UInt32 height;

    ///< fourcc code (platform dependent)
    public UInt32 fourcc;

    ///< frames per second
    public UInt32 fps;

    ///< bits per pixel
    public UInt32 bpp; 
}


public class Native
{
    // supported properties:
    public  const CapPropertyID CAPPROPID_EXPOSURE = 1;
    public  const CapPropertyID CAPPROPID_FOCUS = 2;
    public  const CapPropertyID CAPPROPID_ZOOM = 3;
    public  const CapPropertyID CAPPROPID_WHITEBALANCE = 4;
    public  const CapPropertyID CAPPROPID_GAIN = 5;
    public  const CapPropertyID CAPPROPID_BRIGHTNESS = 6;
    public  const CapPropertyID CAPPROPID_CONTRAST = 7;
    public  const CapPropertyID CAPPROPID_SATURATION = 8;
    public  const CapPropertyID CAPPROPID_GAMMA = 9;
    public  const CapPropertyID CAPPROPID_HUE = 10;
    public  const CapPropertyID CAPPROPID_SHARPNESS = 11;
    public  const CapPropertyID CAPPROPID_BACKLIGHTCOMP = 12;
    public  const CapPropertyID CAPPROPID_POWERLINEFREQ = 13;
    public  const CapPropertyID CAPPROPID_LAST = 14;


    public  const CapResult CAPRESULT_OK = 0;
    public  const CapResult CAPRESULT_ERR = 1;
    public  const CapResult CAPRESULT_DEVICENOTFOUND = 2;
    public  const CapResult CAPRESULT_FORMATNOTSUPPORTED = 3;
    public  const CapResult CAPRESULT_PROPERTYNOTSUPPORTED = 4;


    public  const UInt32 LOG_EMERG = 0;
    public  const UInt32 LOG_ALERT = 1;
    public  const UInt32 LOG_CRIT = 2;
    public  const UInt32 LOG_ERR = 3;
    public  const UInt32 LOG_WARNING = 4;
    public  const UInt32 LOG_NOTICE = 5;
    public  const UInt32 LOG_INFO = 6;
    public  const UInt32 LOG_DEBUG = 7;
    public  const UInt32 LOG_VERBOSE = 8;
 
    private const string DLL_NAME = "openpnp-capture";

    static Native()
    {
        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, ImportResolver);
    }

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        IntPtr libHandle = IntPtr.Zero;
        char Separator = Path.DirectorySeparatorChar;
        if (libraryName == DLL_NAME)
        {
            string dllPath = string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    dllPath = $"libs{Separator}libopenpnp-capture-macos-latest-arm64.dylib";
                }
                else
                {
                    dllPath = $"libs{Separator}libopenpnp-capture-macos-latest-x86_64.dylib";
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    dllPath = $"libs{Separator}libopenpnp-capture-ubuntu-20.04-arm64.so";
                }
                else
                {
                    dllPath = $"libs{Separator}libopenpnp-capture-ubuntu-20.04-x86_64.so";
                }
            }
            else
            { 
                    dllPath = $"libs{Separator}libopenpnp-capture-windows-latest-x86_64.dll";  
            }

            NativeLibrary.TryLoad(dllPath, assembly, DllImportSearchPath.SafeDirectories, out libHandle);
        }

        return libHandle;
    }


    /********************************************************************************** 
        CONTEXT CREATION AND DEVICE ENUMERATION
    **********************************************************************************/

    /** Initialize the capture library
     @return The context ID.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_createContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapContext Cap_createContext();
    
    
    /** Un-initialize the capture library context
    @param ctx The ID of the context to destroy.
    @return The context ID.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_releaseContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern  CapResult Cap_releaseContext(CapContext ctx);

    /** Get the number of capture devices on the system.
    note: this can change dynamically due to the
    pluggin and unplugging of USB devices.
    @param ctx The ID of the context.
    @return The number of capture devices found.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getDeviceCount", CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 Cap_getDeviceCount(CapContext ctx);

    /** Get the name of a capture device.
    This name is meant to be displayed in GUI applications,
    i.e. its human readable.

    if a device with the given index does not exist,
    NULL is returned.
    @param ctx The ID of the context.
    @param index The device index of the capture device.
    @return a pointer to an UTF-8 string containting the name of the capture device.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getDeviceName", CallingConvention = CallingConvention.Cdecl)]
    public static extern String Cap_getDeviceName(CapContext ctx, CapDeviceID index);



    /** Get the unique name of a capture device.
    The string contains a unique concatenation
    of the device name and other parameters.
    These parameters are platform dependent.

    Note: when a USB camera does not expose a serial number,
          platforms might have trouble uniquely identifying 
          a camera. In such cases, the USB port location can
          be used to add a unique feature to the string.
          This, however, has the down side that the ID of
          the camera changes when the USB port location 
          changes. Unfortunately, there isn't much to
          do about this.

    if a device with the given index does not exist,
    NULL is returned.
    @param ctx The ID of the context.
    @param index The device index of the capture device.
    @return a pointer to an UTF-8 string containting the unique ID of the capture device.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getDeviceUniqueID", CallingConvention = CallingConvention.Cdecl)]
    public static extern String Cap_getDeviceUniqueID(CapContext ctx, CapDeviceID index);

    /** Returns the number of formats supported by a certain device.
    returns -1 if device does not exist.

    @param ctx The ID of the context.
    @param index The device index of the capture device.
    @return The number of formats supported or -1 if the device does not exist.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getNumFormats", CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 Cap_getNumFormats(CapContext ctx, CapDeviceID index);


    /** Get the format information from a device. 
    @param ctx The ID of the context.
    @param index The device index of the capture device.
    @param id The index/ID of the frame buffer format (0 .. number returned by Cap_getNumFormats() minus 1 ).
    @param info pointer to a CapFormatInfo structure to be filled with data.
    @return The CapResult.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getFormatInfo", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapResult Cap_getFormatInfo(CapContext ctx, CapDeviceID index, CapFormatID id,
        ref CapFormatInfo info);



    /********************************************************************************** 
         STREAM MANAGEMENT
    **********************************************************************************/

    /** Open a capture stream to a device with specific format requirements  
    Although the (internal) frame buffer format is set via the fourCC ID,
    the frames returned by Cap_captureFrame are always 24-bit RGB. 
    @param ctx The ID of the context.
    @param index The device index of the capture device.
    @param formatID The index/ID of the frame buffer format (0 .. number returned by Cap_getNumFormats() minus 1 ).
    @return The stream ID or -1 if the device does not exist or the stream format ID is incorrect.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_openStream", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapStream Cap_openStream(CapContext ctx, CapDeviceID index, CapFormatID formatID);

    /** Close a capture stream 
    @param ctx The ID of the context.
    @param stream The stream ID.
    @return CapResult
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_closeStream", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapResult Cap_closeStream(CapContext ctx, CapStream stream);


    /** Check if a stream is open, i.e. is capturing data. 
    @param ctx The ID of the context.
    @param stream The stream ID.
    @return 1 if the stream is open and capturing, else 0. 
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_isOpenStream", CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 Cap_isOpenStream(CapContext ctx, CapStream stream);


    /********************************************************************************** 
     FRAME CAPTURING / INFO
    **********************************************************************************/

    /** this function copies the most recent RGB frame data
    to the given buffer.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_captureFrame", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapResult Cap_captureFrame(CapContext ctx, CapStream stream, IntPtr RGBbufferPtr,UInt32 RGBbufferBytes);

    /** returns 1 if a new frame has been captured, 0 otherwise */
    [DllImport(DLL_NAME, EntryPoint = "Cap_hasNewFrame", CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 Cap_hasNewFrame(CapContext ctx, CapStream stream);

    /** returns the number of frames captured during the lifetime of the stream. 
    For debugging purposes */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getStreamFrameCount", CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 Cap_getStreamFrameCount(CapContext ctx, CapStream stream);


    /********************************************************************************** 
     NEW CAMERA CONTROL API FUNCTIONS
    **********************************************************************************/

    /** get the min/max limits and default value of a camera/stream property (e.g. zoom, exposure etc)  
    returns: CAPRESULT_OK if all is well.
             CAPRESULT_PROPERTYNOTSUPPORTED if property not available.
             CAPRESULT_ERR if context, stream are invalid.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getPropertyLimits", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapResult Cap_getPropertyLimits(CapContext ctx, CapStream stream, CapPropertyID propID,
        ref Int32 min, ref Int32 max, ref Int32 dValue);


    /** set the value of a camera/stream property (e.g. zoom, exposure etc)  
    returns: CAPRESULT_OK if all is well.
             CAPRESULT_PROPERTYNOTSUPPORTED if property not available.
             CAPRESULT_ERR if context, stream are invalid.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_setProperty", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapResult Cap_setProperty(CapContext ctx, CapStream stream, CapPropertyID propID, Int32 value);

    /** set the automatic flag of a camera/stream property (e.g. zoom, focus etc)  
    returns: CAPRESULT_OK if all is well.
             CAPRESULT_PROPERTYNOTSUPPORTED if property not available.
             CAPRESULT_ERR if context, stream are invalid.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_setAutoProperty", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapResult Cap_setAutoProperty(CapContext ctx, CapStream stream, CapPropertyID propID,
        UInt32 bOnOff);

    /** get the value of a camera/stream property (e.g. zoom, exposure etc)  
    returns: CAPRESULT_OK if all is well.
             CAPRESULT_PROPERTYNOTSUPPORTED if property not available.
             CAPRESULT_ERR if context, stream are invalid or outValue == NULL.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getProperty", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapResult Cap_getProperty(CapContext ctx, CapStream stream, CapPropertyID propID,
        ref Int32 outValue);

    /** get the automatic flag of a camera/stream property (e.g. zoom, focus etc)  
    returns: CAPRESULT_OK if all is well.
             CAPRESULT_PROPERTYNOTSUPPORTED if property not available.
             CAPRESULT_ERR if context, stream are invalid.
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_getAutoProperty", CallingConvention = CallingConvention.Cdecl)]
    public static extern CapResult Cap_getAutoProperty(CapContext ctx, CapStream stream, CapPropertyID propID,
        ref UInt32 outValue);

    /********************************************************************************** 
     DEBUGGING
    **********************************************************************************/

    /**
    Set the logging level. 
    LOG LEVEL ID  | LEVEL 
    ------------- | -------------
    LOG_EMERG     | 0
    LOG_ALERT     | 1
    LOG_CRIT      | 2
    LOG_ERR       | 3
    LOG_WARNING   | 4
    LOG_NOTICE    | 5
    LOG_INFO      | 6    
    LOG_DEBUG     | 7
    LOG_VERBOSE   | 8
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_setLogLevel", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Cap_setLogLevel(UInt32 level);
    
    
    
    /** install a custom callback for a logging function.
    the callback function must have the following 
    structure:
        void func(uint32_t level, const char *string);
    */
    [DllImport(DLL_NAME, EntryPoint = "Cap_installCustomLogFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Cap_installCustomLogFunction([MarshalAs(UnmanagedType.FunctionPtr)] CapCustomLogFunc logFunc);
    
    /** Return the version of the library as a string.
    In addition to a version number, this should 
    contain information on the platform,
    e.g. Win32/Win64/Linux32/Linux64/OSX etc,
    wether or not it is a release or debug
    build and the build date. 
    When building the library, please set the 
    following defines in the build environment:
    __LIBVER__
    __PLATFORM__
    __BUILDTYPE__ 
    */ 
    [DllImport(DLL_NAME, EntryPoint = "Cap_getLibraryVersion", CallingConvention = CallingConvention.Cdecl)]
    public static extern String Cap_getLibraryVersion();
}