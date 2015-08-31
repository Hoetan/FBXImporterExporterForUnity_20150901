/* Copyright: Copyright 2014 Beijing Noitom Technology Ltd. All Rights reserved.
* Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006
* 
* Licensed under the Neuron SDK License Beta Version (the “License");
* You may only use the Neuron SDK when in compliance with the License,
* which is provided at the time of installation or download, or which
* otherwise accompanies this software in the form of either an electronic or a hard copy.
* 
* Unless required by applicable law or agreed to in writing, the Neuron SDK
* distributed under the License is provided on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing conditions and
* limitations under the License.
*/


using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;  // For DllImport()


namespace NeuronDataReaderWraper
{
    #region Basic data types
    /// <summary>
    /// Socket connection status
    /// </summary>
    public enum SocketStatus
    {
        CS_Running,
        CS_Starting,
        CS_OffWork,
    };

    /// <summary>
    /// Data version
    /// </summary>
    public struct DataVersion
    {
        public byte BuildNumb;         // Build number
        public byte Revision;          // Revision number
        public byte Minor;             // Subversion number
        public byte Major;             // Major version number
    };

    /// <summary>
    /// Header format of BVH data
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct BvhDataHeader
    {
        public ushort HeaderToken1;    // Package start token: 0xDDFF
        public DataVersion DataVersion;// Version of community data format. e.g.: 1.0.0.2
        public UInt32 DataCount;       // Values count, 180 for without disp data
        public UInt32 bWithDisp;       // With/out dispement
        public UInt32 bWithReference;  // With/out reference bone data at first
        public UInt32 AvatarIndex;     // Avatar index
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AvatarName;      // Avatar name
        public UInt32 Reserved1;       // Reserved, only enable this package has 64bytes length
        public UInt32 Reserved2;       // Reserved, only enable this package has 64bytes length
        public ushort HeaderToken2;    // Package end token: 0xEEFF
    };
    #endregion

    #region Command data types    
    /// <summary>
    /// Command identitys
    /// </summary>
    public enum CmdId
    {
	    Cmd_BoneSize,                  // Id used to request bone size from server
	    Cmd_AvatarName,                // Id used to request avatar name from server
	    Cmd_FaceDirection,             // Id used to request face direction from server
        Cmd_DataFrequency,             // Id used to request data sampling frequency from server
        Cmd_BvhInheritance,		       // Id used to request bvh inheritance from server
        Cmd_AvatarCount,	           // Id used to request avatar count from server
        Cmd_CombinationMode,           // 
        Cmd_RegisterEvent,             // 
        Cmd_SetAvatarName,             // 
    };

    // Sensor binding combination mode
    public enum SensorCombinationModes
    {
        SC_ArmOnly,              // Left arm or right arm only
        SC_UpperBody,            // Upper body, include one arm or both arm, must have chest node
        SC_FullBody,             // Full body mode
    };

    /// <summary>
    /// Header format of Command returned from server
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct CommandPack
    {
        public UInt16 Token1;                   // Command start token: 0xAAFF
        public UInt32 DataVersion;              // Version of community data format. e.g.: 1.0.0.2
        public UInt32 DataLength;               // Package length of command data, by byte.
        public UInt32 DataCount;                // Count in data array, related to the specific command.
        public CmdId CommandId;                 // Identity of command.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] CmdParaments;             // Command paraments
        public UInt32 Reserved1;                // Reserved, only enable this package has 32bytes length. Maybe used in the future.
        public UInt16 Token2;                   // Package end token: 0xBBFF
    };


    /// <summary>
    /// Fetched bone size from server
    /// </summary> 
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct CmdResponseBoneSize
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
	    public string BoneName;        // Bone name
	    public float BoneLength;       // Bone length
    };

    #endregion

    #region Callbacks for data output
    /// <summary>
    /// FrameDataReceived CALLBACK
    /// Remarks
    ///   The related information of the data stream can be obtained from BvhDataHeader.
    /// </summary>
    /// <param name="customObject">User defined object.</param>
    /// <param name="sockRef">Connector reference of TCP/IP client as identity.</param>
    /// <param name="bvhDataHeader">A BvhDataHeader type pointer, to output the BVH data format information.</param>
    /// <param name="data">Float type array pointer, to output binary data.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]   
    public delegate void FrameDataReceived(IntPtr customObject, IntPtr sockRef, IntPtr bvhDataHeader, IntPtr data);

    /// <summary>
    /// Callback for command communication data with TCP/IP server
    /// </summary>
    /// <param name="customedObj">User defined object.</param>
    /// <param name="sockRef">Connector reference of TCP/IP client as identity.</param>
    /// <param name="cmdHeader">A CommandHeader type pointer contains command data information.</param>
    /// <param name="cmdData">Data pointer of command, related to the specific command.</param>
    /// <remark>The related information of the command data can be obtained from CommandHeader. The data content is identified by its command id.</remark>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void CommandDataReceived(IntPtr customedObj, IntPtr sockRef, IntPtr cmdHeader, IntPtr cmdData);

    /// <summary>
    /// SocketStatusChanged CALLBACK
    /// Remarks
    ///   As convenient, use BRGetSocketStatus() to get status manually other than register this callback
    /// </summary>
    /// <param name="customObject">User defined object.</param>
    /// <param name="sockRef">Socket reference of TCP or UDP service identity.</param>
    /// <param name="bvhDataHeader">Socket connection status</param>
    /// <param name="data">Socket status description.</param>
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]   
    public delegate void SocketStatusChanged(IntPtr customObject, IntPtr sockRef, SocketStatus status, [MarshalAs(UnmanagedType.LPStr)]string msg);
    #endregion

    // API exportor
    public class NeuronDataReader
    {
        #region Importor definition
#if UNITY_IPHONE && !UNITY_EDITOR
		private const string ReaderImportor = "__Internal";
#elif _WINDOWS
		private const string ReaderImportor = "NeuronDataReader.dll";
#else
        private const string ReaderImportor = "NeuronDataReader";
#endif
        #endregion

        #region Functions API
        /// <summary>
        /// Register receiving and parsed frame data callback
        /// </summary>
        /// <param name="customedObj">Client defined object. Can be null</param>
        /// <param name="handle">Client defined function.</param>
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void BRRegisterFrameDataCallback(IntPtr customedObj, FrameDataReceived handle);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        //[return: MarshalAs(UnmanagedType.LPStr)]
		private static extern IntPtr BRGetLastErrorMessage();
		/// <summary>
		/// Call this function to get what error occured in library.
		/// </summary>
		/// <returns></returns>
        public static string strBRGetLastErrorMessage()
        {
            // Get message pointer
            IntPtr ptr = BRGetLastErrorMessage();
            // Construct a string from the pointer.
			return Marshal.PtrToStringAnsi(ptr);
        }

        // Register TCP socket status callback
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void BRRegisterSocketStatusCallback(IntPtr customedObj, SocketStatusChanged handle);

        // Connect to server by TCP/IP
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr BRConnectTo(string serverIP, int nPort);

        // Check TCP/UDP service status
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern SocketStatus BRGetSocketStatus(IntPtr sockRef);

        // Close a TCP/UDP service
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void BRCloseSocket(IntPtr sockRef);

        // Start a UDP service to receive data at 'nPort'
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr BRStartUDPServiceAt(int nPort);
        #endregion

        #region Commands API
        /// <summary>
        /// Register receiving and parsed Cmd data callback
        /// </summary>
        /// <param name="customedObj">Client defined object. Can be null</param>
        /// <param name="handle">Client defined function.</param>
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void BRRegisterCommandDataCallback(IntPtr customedObj, CommandDataReceived handle);

        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool BRRegisterAutoSyncParmeter(IntPtr sockRef, CmdId cmdId);

        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool BRUnregisterAutoSyncParmeter(IntPtr sockRef, CmdId cmdId);

        // Check TCP connect status
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool BRCommandFetchAvatarDataFromServer(IntPtr sockRef, int avatarIndex, CmdId cmdId);

        // Check TCP connect status
        [DllImport(ReaderImportor, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool BRCommandFetchDataFromServer(IntPtr sockRef, CmdId cmdId);
        #endregion
    }
}
