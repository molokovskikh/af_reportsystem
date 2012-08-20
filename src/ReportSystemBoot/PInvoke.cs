using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Inforoom.WindowsService.Security;

namespace Inforoom.WindowsService
{
	public class GenericHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public GenericHandle()
			: base(true)
		{
		}

		protected override bool ReleaseHandle()
		{
			return Win32.CloseHandle(handle);
		}
	}

	public class WindowStationHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public WindowStationHandle()
			: base(true)
		{
		}

		protected override bool ReleaseHandle()
		{
			return Win32.CloseWindowStation(handle);
		}
	}

	public class DesktopHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public DesktopHandle()
			: base(true)
		{
		}

		protected override bool ReleaseHandle()
		{
			return Win32.CloseDesktop(handle);
		}
	}

	public enum ACL_INFORMATION_CLASS
	{
		AclRevisionInformation = 1,
		AclSizeInformation
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ACCESS_ALLOWED_ACE
	{
		public ACE_HEADER Header;
		public ACCESS_MASK Mask;
		public int SidStart;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ACE_HEADER
	{
		public byte AceType;
		public byte AceFlags;
		public short AceSize;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ACL_SIZE_INFORMATION
	{
		public uint AceCount;
		public uint AclBytesInUse;
		public uint AclBytesFree;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SECURITY_DESCRIPTOR
	{
		public byte Revision;
		public byte Sbz1;
		public int Control;
		public int Owner;
		public int Group;
		public ACL Sacl;
		public ACL Dacl;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ACL
	{
		public byte AclRevision;
		public byte Sbz1;
		public short AclSize;
		public short AceCount;
		public short Sbz2;
	}

	public enum TOKEN_INFORMATION_CLASS : uint
	{
		TokenUser = 1,
		TokenGroups,
		TokenPrivileges,
		TokenOwner,
		TokenPrimaryGroup,
		TokenDefaultDacl,
		TokenSource,
		TokenType,
		TokenImpersonationLevel,
		TokenStatistics,
		TokenRestrictedSids,
		TokenSessionId,
		TokenGroupsAndPrivileges,
		TokenSessionReference,
		TokenSandBoxInert,
		TokenAuditPolicy,
		TokenOrigin,
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TOKEN_USER
	{
		public SID_AND_ATTRIBUTES User;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class TOKEN_GROUPS
	{
		public uint GroupCount;
		public SID_AND_ATTRIBUTES[] Group;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SID_AND_ATTRIBUTES
	{
		public IntPtr Sid;
		public uint Attributes;
	}

	[Flags]
	internal enum CreationFlags
	{
		CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
		CREATE_DEFAULT_ERROR_MODE = 0x04000000,
		CREATE_NEW_CONSOLE = 0x00000010,
		CREATE_NEW_PROCESS_GROUP = 0x00000200,
		CREATE_NO_WINDOW = 0x08000000,
		CREATE_PROTECTED_PROCESS = 0x00040000,
		CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
		CREATE_SEPARATE_WOW_VDM = 0x00001000,
		CREATE_SUSPENDED = 0x00000004,
		CREATE_UNICODE_ENVIRONMENT = 0x00000400,
		DEBUG_ONLY_THIS_PROCESS = 0x00000002,
		DEBUG_PROCESS = 0x00000001,
		DETACHED_PROCESS = 0x00000008,
		EXTENDED_STARTUPINFO_PRESENT = 0x00080000
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct STARTUPINFO
	{
		public Int32 cb;
		public string lpReserved;
		public string lpDesktop;
		public string lpTitle;
		public Int32 dwX;
		public Int32 dwY;
		public Int32 dwXSize;
		public Int32 dwYSize;
		public Int32 dwXCountChars;
		public Int32 dwYCountChars;
		public Int32 dwFillAttribute;
		public Int32 dwFlags;
		public Int16 wShowWindow;
		public Int16 cbReserved2;
		public IntPtr lpReserved2;
		public IntPtr hStdInput;
		public IntPtr hStdOutput;
		public IntPtr hStdError;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class SECURITY_ATTRIBUTES
	{
		public int nLength;
		public IntPtr lpSecurityDescriptor;
		public int bInheritHandle;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct PROCESS_INFORMATION
	{
		public IntPtr hProcess;
		public IntPtr hThread;
		public int dwProcessId;
		public int dwThreadId;
	}

	public class Win32
	{
		public const int LOGON32_PROVIDER_DEFAULT = 0;
		public const int LOGON32_LOGON_INTERACTIVE = 2;

		public const int READ_CONTROL = 0x00020000;
		public const int WRITE_DAC = 0x00040000;

		public const int DESKTOP_WRITEOBJECTS = 0x0080;
		public const int DESKTOP_READOBJECTS = 0x0001;

		public const int NORMAL_PRIORITY_CLASS = 0x00000020;
		public const int CREATE_NEW_CONSOLE = 0x00000010;

		public const uint SE_GROUP_LOGON_ID = 0xC0000000;

		public const int DACL_SECURITY_INFORMATION = 0x00000004;

		public const int SECURITY_DESCRIPTOR_REVISION = 1;

		public const int HEAP_ZERO_MEMORY = 0x00000008;

		public const int ACL_REVISION = 2;

		public const int ACCESS_ALLOWED_ACE_TYPE = 0;
		public const int OBJECT_INHERIT_ACE = 1;
		public const int CONTAINER_INHERIT_ACE = 2;
		public const int NO_PROPAGATE_INHERIT_ACE = 4;
		public const int INHERIT_ONLY_ACE = 8;

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool LogonUser(string username,
			string domain,
			string password,
			int dwLogonType,
			int dwLogonProvider,
			ref GenericHandle phToken);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool ImpersonateLoggedOnUser(GenericHandle hToken);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool RevertToSelf();

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr handle);

		[DllImport("user32", SetLastError = true)]
		public static extern WindowStationHandle OpenWindowStation(
			string lpszWinSta,
			bool fInherit,
			ACCESS_MASK dwDesiredAccess);

		[DllImport("user32", SetLastError = true)]
		public static extern bool CloseWindowStation(IntPtr hWinsta);

		[DllImport("user32", SetLastError = true)]
		public static extern bool SetProcessWindowStation(WindowStationHandle windowStation);

		[DllImport("user32", SetLastError = true)]
		public static extern DesktopHandle OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, ACCESS_MASK dwDesiredAccess);

		[DllImport("user32", SetLastError = true)]
		public static extern bool CloseDesktop(IntPtr hDesktop);

		[DllImport("user32", SetLastError = true)]
		public static extern WindowStationHandle GetProcessWindowStation();

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool CreateProcessAsUser(
			GenericHandle hToken,
			string lpApplicationName,
			string lpCommandLine,
			IntPtr lpProcessAttributes,
			IntPtr lpThreadAttributes,
			bool bInheritHandles,
			uint dwCreationFlags,
			IntPtr lpEnvironment,
			string lpCurrentDirectory,
			ref STARTUPINFO lpStartupInfo,
			out PROCESS_INFORMATION lpProcessInformation);

		// Using IntPtr for pSID insted of Byte[]
		[DllImport("advapi32", SetLastError = true)]
		public static extern bool ConvertSidToStringSid(IntPtr pSID, out IntPtr ptrSid);

		[DllImport("kernel32")]
		public static extern IntPtr LocalFree(IntPtr hMem);

		[DllImport("kernel32")]
		public static extern IntPtr GetCurrentThreadId();

		[DllImport("user32", SetLastError = true)]
		public static extern DesktopHandle GetThreadDesktop(IntPtr thread);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool GetTokenInformation(
			SafeHandle TokenHandle,
			TOKEN_INFORMATION_CLASS TokenInformationClass,
			IntPtr TokenInformation,
			int TokenInformationLength,
			out int ReturnLength);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool GetTokenInformation(
			IntPtr TokenHandle,
			TOKEN_INFORMATION_CLASS TokenInformationClass,
			IntPtr TokenInformation,
			int TokenInformationLength,
			out int ReturnLength);


		[DllImport("user32", SetLastError = true)]
		private static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex,
			[Out] byte[] pvInfo, uint nLength, out uint lpnLengthNeeded);

		[DllImport("user32", SetLastError = true)]
		public static extern bool GetUserObjectSecurity(SafeHandle hObj, ref int siRequested, IntPtr securityDescriptor, int length, out int lengthNeeded);

		[DllImport("user32", SetLastError = true)]
		public static extern bool GetUserObjectSecurity(SafeHandle hObj, ref int siRequested, byte[] securityDescriptor, int length, out int lengthNeeded);

		[DllImport("user32", SetLastError = true)]
		public static extern bool SetUserObjectSecurity(SafeHandle hObj, ref int siRequested, IntPtr securityDescriptor);

		[DllImport("user32", SetLastError = true)]
		public static extern bool SetUserObjectSecurity(SafeHandle hObj, ref int siRequested, byte[] securityDescriptor);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool InitializeSecurityDescriptor(IntPtr pSecurityDescriptor, int dwRevision);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool GetSecurityDescriptorDacl(IntPtr pSecurityDescriptor,
			out bool lpbDaclPresent,
			ref IntPtr pDacl,
			out bool lpbDaclDefaulted);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool GetAclInformation(IntPtr acl,
			ref ACL_SIZE_INFORMATION aclInformation,
			int aclInformationLength,
			ACL_INFORMATION_CLASS aclInformationClass);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool InitializeAcl(IntPtr acl, int aclLength, int aclRevision);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool GetAce(IntPtr acl, int aceIndex, out IntPtr ace);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool AddAce(IntPtr acl, int dwAceRevision, int startingAceIndex, IntPtr aceList, int aceListLength);

		[DllImport("advapi32", SetLastError = true, EntryPoint = "AddAce")]
		public static extern bool AddAce2(IntPtr acl, int dwAceRevision, int startingAceIndex, ref ACCESS_ALLOWED_ACE aceList, int aceListLength);

		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool AddAccessAllowedAce(IntPtr acl, uint aceRevision, ACCESS_MASK accessMask, IntPtr sid);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool SetSecurityDescriptorDacl(IntPtr securityDescriptor, bool daclPresent, IntPtr dacl, bool daclDefaulted);

		[DllImport("advapi32")]
		public static extern uint GetLengthSid(IntPtr sid);

		[DllImport("advapi32", SetLastError = true)]
		public static extern bool CopySid(uint destinationSidLength, IntPtr destinationSid, IntPtr sourceSid);

		[DllImport("User32", SetLastError = true)]
		public static extern bool SetThreadDesktop(DesktopHandle desktop);
	}
}