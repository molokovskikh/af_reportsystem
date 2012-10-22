using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Inforoom.WindowsService;
using Inforoom.WindowsService.Security;
using ProcessPrivileges;

namespace Inforoom.WindowsService
{
	public class ProcessStarter
	{
		public static void StartProcessInteractivly(string commandLine, string username, string password, string domain)
		{
/*
			var currentProcess = Process.GetCurrentProcess();

			currentProcess.EnablePrivilege(Privilege.IncreaseQuota);
			currentProcess.EnablePrivilege(Privilege.AssignPrimaryToken);

			var p1 = currentProcess.GetPrivilegeState(Privilege.AssignPrimaryToken);
			var p2 = currentProcess.GetPrivilegeState(Privilege.IncreaseQuota);

			if (p1 == PrivilegeState.Removed)
				throw new Exception(String.Format(@"Привелегия AssignPrimaryToken удалена проверь групповую политику, пользователь {0} должен быть добавлен в политику ""Замена маркера уровня процесса"" "
					+ "gpedit.msc Конфигурация Windows\\Параметры безопасности\\Локальные политики\\Назначение прав пользователя ",
					username));

			if (p2 == PrivilegeState.Removed)
				throw new Exception(String.Format(@"Привелегия IncreaseQuota удалена проверь групповую политику, пользователь {0} должен быть добавлен в политику ""Настройка квот памяти для процесса"" "
					+ "gpedit.msc Конфигурация Windows\\Параметры безопасности\\Локальные политики\\Назначение прав пользователя ",
					username));
 */

			var user = new GenericHandle();
			if (!Win32.LogonUser(username, domain, password, Win32.LOGON32_LOGON_INTERACTIVE, Win32.LOGON32_PROVIDER_DEFAULT, ref user))
				throw new Win32Exception();

			var originalWindowStation = Win32.GetProcessWindowStation();

			var windowStation = Win32.OpenWindowStation("Winsta0", false,
				ACCESS_MASK.READ_CONTROL
					| ACCESS_MASK.WRITE_DAC);
			if (windowStation.IsInvalid)
				throw new Win32Exception();

			try {
				if (!Win32.SetProcessWindowStation(windowStation))
					throw new Win32Exception();


				var desktop = Win32.OpenDesktop("default", 0, false,
					ACCESS_MASK.READ_CONTROL
						| ACCESS_MASK.WRITE_DAC
						| ACCESS_MASK.DESKTOP_READOBJECTS
						| ACCESS_MASK.DESKTOP_WRITEOBJECTS);
				if (desktop.IsInvalid)
					throw new Win32Exception();

				if (!Win32.ImpersonateLoggedOnUser(user))
					throw new Win32Exception();

				//AssignSecurity3(windowStation, desktop, user);
				var sid = new SecurityIdentifier(TokenToSid(user));
				AssignSecurity4(windowStation, desktop, sid);
				//По завершении работы процесса нужно удалить ace
				//из station acl и desktop acl
				//если этого не делать через какоето время получим ошибку
				//ERROR_NOT_ENOUGH_QUOTA подробней http://support.microsoft.com/kb/185292
				var pid = StartProcessInteractivly(user, commandLine);
				Process.GetProcessById(pid).WaitForExit();
				ReleaseSecurity(windowStation, desktop, sid);
			}
			finally {
				Win32.RevertToSelf();
				Win32.SetProcessWindowStation(originalWindowStation);
			}
		}

		public static void AssignSecurity4(WindowStationHandle station,
			DesktopHandle desktop,
			SecurityIdentifier sid)
		{
			var stationSecurity = new GenericSecurity(true,
				ResourceType.WindowObject,
				station,
				AccessControlSections.Access);

			stationSecurity.AddAccessRule(new GenericAccessRule(
				sid,
				(int)ACCESS_MASK.GENERIC_ALL,
				InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
				PropagationFlags.InheritOnly,
				AccessControlType.Allow));
			stationSecurity.AddAccessRule(new GenericAccessRule(sid,
				(int)ACCESS_MASK.WINSTA_ALL,
				AccessControlType.Allow));
			stationSecurity.Persist(station);

			var desktopSecurity = new GenericSecurity(true, ResourceType.WindowObject, desktop, AccessControlSections.Access);
			desktopSecurity.AddAccessRule(new GenericAccessRule(sid, (int)ACCESS_MASK.DESKTOP_ALL, AccessControlType.Allow));
			desktopSecurity.Persist(desktop);
		}

		public static void ReleaseSecurity(WindowStationHandle station,
			DesktopHandle desktop,
			SecurityIdentifier sid)
		{
			var desktopSecurity = new GenericSecurity(true,
				ResourceType.WindowObject,
				desktop,
				AccessControlSections.Access);
			foreach (GenericAccessRule rule in desktopSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier))) {
				if (rule.IdentityReference == sid)
					desktopSecurity.RemoveAccessRule(rule);
			}
			desktopSecurity.Persist(desktop);

			var stationSecurity = new GenericSecurity(true,
				ResourceType.WindowObject,
				station,
				AccessControlSections.Access);
			foreach (GenericAccessRule rule in stationSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier))) {
				if (rule.IdentityReference == sid)
					stationSecurity.RemoveAccessRule(rule);
			}
			stationSecurity.Persist(station);
		}

		private static void AssignSecurity3(WindowStationHandle station,
			DesktopHandle desktop,
			GenericHandle user)
		{
			var sid = TokenToSid(user);
			AssignStationSecurity(station, sid);
			AssignDesktopSecutiry(desktop, sid);
		}

		private static void AssignStationSecurity(WindowStationHandle station, IntPtr sid)
		{
			var descriptor = IntPtr.Zero;
			var newDescriptor = IntPtr.Zero;
			var descriptorLength = 0;
			var lengthNeeded = 0;
			var siRequested = Win32.DACL_SECURITY_INFORMATION;
			Win32.GetUserObjectSecurity(station, ref siRequested, descriptor, descriptorLength, out lengthNeeded);


			descriptor = Marshal.AllocHGlobal(lengthNeeded);
			descriptorLength = lengthNeeded;

			if (!Win32.GetUserObjectSecurity(station, ref siRequested, descriptor, descriptorLength, out lengthNeeded))
				throw new Win32Exception();

			newDescriptor = Marshal.AllocHGlobal(lengthNeeded);
			if (!Win32.InitializeSecurityDescriptor(newDescriptor, Win32.SECURITY_DESCRIPTOR_REVISION))
				throw new Win32Exception();

			bool daclPresent;
			bool daclExists;
			var acl = IntPtr.Zero;
			if (!Win32.GetSecurityDescriptorDacl(descriptor, out daclPresent, ref acl, out daclExists))
				throw new Win32Exception();

			var aclSize = new ACL_SIZE_INFORMATION {
				AclBytesInUse = (uint)Marshal.SizeOf(typeof(ACL))
			};
			if (acl != IntPtr.Zero) {
				if (!Win32.GetAclInformation(acl, ref aclSize, Marshal.SizeOf(aclSize), ACL_INFORMATION_CLASS.AclSizeInformation))
					throw new Win32Exception();
			}

			var aclLenth = (int)(aclSize.AclBytesInUse
				+ 2 * Marshal.SizeOf(typeof(ACCESS_ALLOWED_ACE))
				+ 2 * Win32.GetLengthSid(sid)
				- 2 * Marshal.SizeOf(typeof(int)));
			var newAcl = Marshal.AllocHGlobal(aclLenth);

			if (!Win32.InitializeAcl(newAcl, aclLenth, Win32.ACL_REVISION))
				throw new Win32Exception();

			if (daclPresent) {
				for (var i = 0; i < aclSize.AceCount; i++) {
					var ace = IntPtr.Zero;
					if (!Win32.GetAce(acl, i, out ace))
						throw new Win32Exception();
					var aceSize = Marshal.ReadInt16(ace, 2);
					if (!Win32.AddAce(newAcl, Win32.ACL_REVISION, int.MaxValue, ace, aceSize))
						throw new Win32Exception();
				}
			}

			var newAceSize = (int)(Marshal.SizeOf(typeof(ACCESS_ALLOWED_ACE))
				+ Win32.GetLengthSid(sid)
				- Marshal.SizeOf(typeof(int)));
			var newAce = Marshal.AllocHGlobal(newAceSize);

			Marshal.WriteByte(newAce, 0, Win32.ACCESS_ALLOWED_ACE_TYPE);
			Marshal.WriteByte(newAce, 1, Win32.INHERIT_ONLY_ACE | Win32.CONTAINER_INHERIT_ACE | Win32.OBJECT_INHERIT_ACE);
			Marshal.WriteInt16(newAce, 2, (short)newAceSize);
			Marshal.WriteInt32(newAce, 4, (int)ACCESS_MASK.GENERIC_ALL);
			Win32.CopySid(Win32.GetLengthSid(sid), (IntPtr)((long)newAce + 8), sid);
			//var newAce = new ACCESS_ALLOWED_ACE
			//                {
			//                    Header =
			//                        {
			//                            AceType = Win32.ACCESS_ALLOWED_ACE_TYPE,
			//                            AceFlags = Win32.INHERIT_ONLY_ACE | Win32.CONTAINER_INHERIT_ACE | Win32.OBJECT_INHERIT_ACE,
			//                            AceSize = (short) Marshal.SizeOf(typeof(ACCESS_ALLOWED_ACE)),
			//                        },
			//                    Mask = ACCESS_MASK.GENERIC_ACCESS,
			//                };

			if (!Win32.AddAce(newAcl, Win32.ACL_REVISION, int.MaxValue, newAce, newAceSize))
				throw new Win32Exception();


			Marshal.WriteByte(newAce, 1, Win32.NO_PROPAGATE_INHERIT_ACE);
			Marshal.WriteInt32(newAce, 4, (int)ACCESS_MASK.WINSTA_ALL);

			if (!Win32.AddAce(newAcl, Win32.ACL_REVISION, int.MaxValue, newAce, newAceSize))
				throw new Win32Exception();

			if (!Win32.SetSecurityDescriptorDacl(newDescriptor, true, newAcl, false))
				throw new Win32Exception();

			if (!Win32.SetUserObjectSecurity(station, ref siRequested, newDescriptor))
				throw new Win32Exception();
		}

		private static void AssignDesktopSecutiry(DesktopHandle desktop, IntPtr sid)
		{
			var descriptor = IntPtr.Zero;
			var newDescriptor = IntPtr.Zero;
			var descriptorLength = 0;
			var lengthNeeded = 0;
			var siRequested = Win32.DACL_SECURITY_INFORMATION;
			Win32.GetUserObjectSecurity(desktop, ref siRequested, descriptor, descriptorLength, out lengthNeeded);

			descriptor = Marshal.AllocHGlobal(lengthNeeded);
			descriptorLength = lengthNeeded;

			if (!Win32.GetUserObjectSecurity(desktop, ref siRequested, descriptor, descriptorLength, out lengthNeeded))
				throw new Win32Exception();

			newDescriptor = Marshal.AllocHGlobal(lengthNeeded);
			if (!Win32.InitializeSecurityDescriptor(newDescriptor, Win32.SECURITY_DESCRIPTOR_REVISION))
				throw new Win32Exception();

			bool daclPresent;
			bool daclExists;
			var acl = IntPtr.Zero;
			if (!Win32.GetSecurityDescriptorDacl(descriptor, out daclPresent, ref acl, out daclExists))
				throw new Win32Exception();

			var aclSize = new ACL_SIZE_INFORMATION {
				AclBytesInUse = (uint)Marshal.SizeOf(typeof(ACL))
			};
			if (acl != IntPtr.Zero) {
				if (!Win32.GetAclInformation(acl, ref aclSize, Marshal.SizeOf(aclSize), ACL_INFORMATION_CLASS.AclSizeInformation))
					throw new Win32Exception();
			}

			var aclLenth = (int)(aclSize.AclBytesInUse
				+ Marshal.SizeOf(typeof(ACCESS_ALLOWED_ACE))
				+ Win32.GetLengthSid(sid)
				- Marshal.SizeOf(typeof(int)));
			var newAcl = Marshal.AllocHGlobal(aclLenth);

			if (!Win32.InitializeAcl(newAcl, aclLenth, Win32.ACL_REVISION))
				throw new Win32Exception();

			if (daclPresent) {
				for (var i = 0; i < aclSize.AceCount; i++) {
					var ace = IntPtr.Zero;
					if (!Win32.GetAce(acl, i, out ace))
						throw new Win32Exception();
					var aceSize = Marshal.ReadInt16(ace, 2);
					if (!Win32.AddAce(newAcl, Win32.ACL_REVISION, int.MaxValue, ace, aceSize))
						throw new Win32Exception();
				}
			}

			if (!Win32.AddAccessAllowedAce(newAcl, Win32.ACL_REVISION, ACCESS_MASK.DESKTOP_ALL, sid))
				throw new Win32Exception();

			if (!Win32.SetSecurityDescriptorDacl(newDescriptor, true, newAcl, false))
				throw new Win32Exception();

			if (!Win32.SetUserObjectSecurity(desktop, ref siRequested, newDescriptor))
				throw new Win32Exception();
		}

		private static int StartProcessInteractivly(GenericHandle user, string commandLine)
		{
			var startupInfo = new STARTUPINFO();
			startupInfo.cb = Marshal.SizeOf(startupInfo);
			startupInfo.lpDesktop = @"Winsta0\default";
			var processInfo = new PROCESS_INFORMATION();

			SECURITY_ATTRIBUTES attributes = null;
			SECURITY_ATTRIBUTES securityAttributes = null;
			if (!Win32.CreateProcessAsUser(user,
				null,
				commandLine,
				IntPtr.Zero,
				IntPtr.Zero,
				false,
				0,
				IntPtr.Zero,
				null,
				ref startupInfo,
				out processInfo))
				throw new Win32Exception();
			return processInfo.dwProcessId;
		}

		public static IntPtr TokenToSid(SafeHandle user)
		{
			int tokenInfLength;
			Win32.GetTokenInformation(user,
				TOKEN_INFORMATION_CLASS.TokenGroups,
				IntPtr.Zero,
				0,
				out tokenInfLength);

			var tokenInformation = Marshal.AllocHGlobal(tokenInfLength);
			try {
				if (!Win32.GetTokenInformation(user,
					TOKEN_INFORMATION_CLASS.TokenGroups,
					tokenInformation,
					tokenInfLength,
					out tokenInfLength))
					throw new Win32Exception();

				var size = Marshal.ReadInt32(tokenInformation);
				var offset = IntPtr.Size;
				var structure = typeof(SID_AND_ATTRIBUTES);
				var ofsize = Marshal.SizeOf(structure);
				var array = new SID_AND_ATTRIBUTES[size];
				for (var i = 0; i < size; i++) {
					var ptr = new IntPtr((long)tokenInformation + offset);
					array[i] = (SID_AND_ATTRIBUTES)Marshal.PtrToStructure(ptr, structure);
					offset += ofsize;
				}
				foreach (var attributes in array) {
					if ((attributes.Attributes & Win32.SE_GROUP_LOGON_ID) > 0)
						return attributes.Sid;
				}
			}
			finally {
				Marshal.Release(tokenInformation);
			}
			throw new Exception("Не нашел sid сесии");
		}
	}
}