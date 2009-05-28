using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Inforoom.WindowsService.Security
{
	[Flags]
	public enum ACCESS_MASK : uint
	{
		DELETE = 0x00010000,
		READ_CONTROL = 0x00020000,
		WRITE_DAC = 0x00040000,
		WRITE_OWNER = 0x00080000,
		SYNCHRONIZE = 0x00100000,

		STANDARD_RIGHTS_REQUIRED = 0x000f0000,

		STANDARD_RIGHTS_READ = 0x00020000,
		STANDARD_RIGHTS_WRITE = 0x00020000,
		STANDARD_RIGHTS_EXECUTE = 0x00020000,

		STANDARD_RIGHTS_ALL = 0x001f0000,

		SPECIFIC_RIGHTS_ALL = 0x0000ffff,

		ACCESS_SYSTEM_SECURITY = 0x01000000,

		MAXIMUM_ALLOWED = 0x02000000,

		GENERIC_READ = 0x80000000,
		GENERIC_WRITE = 0x40000000,
		GENERIC_EXECUTE = 0x20000000,
		GENERIC_ALL = 0x10000000,

		GENERIC_ACCESS = GENERIC_READ | GENERIC_WRITE |
						 GENERIC_EXECUTE | GENERIC_ALL,

		DESKTOP_READOBJECTS = 0x00000001,
		DESKTOP_CREATEWINDOW = 0x00000002,
		DESKTOP_CREATEMENU = 0x00000004,
		DESKTOP_HOOKCONTROL = 0x00000008,
		DESKTOP_JOURNALRECORD = 0x00000010,
		DESKTOP_JOURNALPLAYBACK = 0x00000020,
		DESKTOP_ENUMERATE = 0x00000040,
		DESKTOP_WRITEOBJECTS = 0x00000080,
		DESKTOP_SWITCHDESKTOP = 0x00000100,

		DESKTOP_ALL = DESKTOP_READOBJECTS | DESKTOP_CREATEWINDOW |
					  DESKTOP_CREATEMENU | DESKTOP_HOOKCONTROL | DESKTOP_JOURNALRECORD |
					  DESKTOP_JOURNALPLAYBACK | DESKTOP_ENUMERATE | DESKTOP_WRITEOBJECTS |
					  DESKTOP_SWITCHDESKTOP | STANDARD_RIGHTS_REQUIRED,

		WINSTA_ENUMDESKTOPS = 0x00000001,
		WINSTA_READATTRIBUTES = 0x00000002,
		WINSTA_ACCESSCLIPBOARD = 0x00000004,
		WINSTA_CREATEDESKTOP = 0x00000008,
		WINSTA_WRITEATTRIBUTES = 0x00000010,
		WINSTA_ACCESSGLOBALATOMS = 0x00000020,
		WINSTA_EXITWINDOWS = 0x00000040,
		WINSTA_ENUMERATE = 0x00000100,
		WINSTA_READSCREEN = 0x00000200,

		WINSTA_ALL = WINSTA_ENUMDESKTOPS | WINSTA_READATTRIBUTES |
					 WINSTA_ACCESSCLIPBOARD | WINSTA_CREATEDESKTOP |
					 WINSTA_WRITEATTRIBUTES | WINSTA_ACCESSGLOBALATOMS |
					 WINSTA_EXITWINDOWS | WINSTA_ENUMERATE | WINSTA_READSCREEN |
					 STANDARD_RIGHTS_REQUIRED,

		WINSTA_ALL_ACCESS = 0x0000037f
	}

	public sealed class GenericAccessRule : AccessRule
	{
		public GenericAccessRule(IdentityReference identity, int accessRights, AccessControlType accessType)
			: base(identity, accessRights, false, InheritanceFlags.None, PropagationFlags.None, accessType)
		{
		}

		public GenericAccessRule(IdentityReference identity, int accessRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
			AccessControlType accessType)
			: base(identity, accessRights, false, inheritanceFlags, propagationFlags, accessType)
		{
		}

		public GenericAccessRule(IdentityReference identity, int accessRights, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
			AccessControlType accessType)
			: base(identity, accessRights, isInherited, inheritanceFlags, propagationFlags, accessType)
		{
		}

		public int AccessRights { get { return AccessMask; } }
	}

	public sealed class GenericAuditRule : AuditRule
	{
		public GenericAuditRule(IdentityReference identity, int accessRights, AuditFlags auditFlags)
			: base(identity, accessRights, false, InheritanceFlags.None, PropagationFlags.None, auditFlags)
		{
		}

		public GenericAuditRule(IdentityReference identity, int accessRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
			AuditFlags auditFlags)
			: base(identity, accessRights, false, inheritanceFlags, propagationFlags, auditFlags)
		{
		}

		public GenericAuditRule(IdentityReference identity, int accessRights, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags,
			AuditFlags auditFlags)
			: base(identity, accessRights, isInherited, inheritanceFlags, propagationFlags, auditFlags)
		{
		}

		public int AccessRights { get { return AccessMask; } }
	}

	public sealed class GenericSecurity : NativeObjectSecurity
	{
		public GenericSecurity(bool isContainer, ResourceType resourceType, SafeHandle handle)
			: base(isContainer, resourceType, handle, AccessControlSections.Access | AccessControlSections.Group | AccessControlSections.Owner)
		{
		}

		public GenericSecurity(bool isContainer, ResourceType resourceType, SafeHandle handle, AccessControlSections includeSections)
			: base(isContainer, resourceType, handle, includeSections)
		{
		}

		public GenericSecurity(bool isContainer, ResourceType resourceType, string name)
			: base(isContainer, resourceType, name, AccessControlSections.Access | AccessControlSections.Group | AccessControlSections.Owner)
		{
		}

		public GenericSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections)
			: base(isContainer, resourceType, name, includeSections)
		{
		}

		public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
			PropagationFlags propagationFlags, AccessControlType accessType)
		{
			return new GenericAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, accessType);
		}

		public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags,
			PropagationFlags propagationFlags, AuditFlags auditFlags)
		{
			return new GenericAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, auditFlags);
		}

		public void AddAccessRule(GenericAccessRule rule)
		{
			base.AddAccessRule(rule);
		}

		public void AddAuditRule(GenericAuditRule rule)
		{
			base.AddAuditRule(rule);
		}

		public bool RemoveAccessRule(GenericAccessRule rule)
		{
			return base.RemoveAccessRule(rule);
		}

		public void RemoveAccessRuleAll(GenericAccessRule rule)
		{
			base.RemoveAccessRuleAll(rule);
		}

		public void RemoveAccessRuleSpecific(GenericAccessRule rule)
		{
			base.RemoveAccessRuleSpecific(rule);
		}

		public bool RemoveAuditRule(GenericAuditRule rule)
		{
			return base.RemoveAuditRule(rule);
		}

		public void RemoveAuditRuleAll(GenericAuditRule rule)
		{
			base.RemoveAuditRuleAll(rule);
		}

		public void RemoveAuditRuleSpecific(GenericAuditRule rule)
		{
			base.RemoveAuditRuleSpecific(rule);
		}

		public void ResetAccessRule(GenericAccessRule rule)
		{
			base.ResetAccessRule(rule);
		}

		public void SetAccessRule(GenericAccessRule rule)
		{
			base.SetAccessRule(rule);
		}

		public void SetAuditRule(GenericAuditRule rule)
		{
			base.SetAuditRule(rule);
		}

		public void Persist(SafeHandle handle)
		{
			WriteLock();
			try
			{
				var sectionsModified = GetAccessControlSectionsModified();
				if (sectionsModified != AccessControlSections.None)
				{
					Persist(handle, sectionsModified);
					ResetAccessControlSectionsModified();
				}
			}
			finally
			{
				WriteUnlock();
			}
		}

		public void Persist(string name)
		{
			WriteLock();
			try
			{
				AccessControlSections sectionsModified = GetAccessControlSectionsModified();
				if (sectionsModified != AccessControlSections.None)
				{
					Persist(name, sectionsModified);
					ResetAccessControlSectionsModified();
				}
			}
			finally
			{
				WriteUnlock();
			}
		}

		private AccessControlSections GetAccessControlSectionsModified()
		{
			AccessControlSections sectionsModified = AccessControlSections.None;
			if (AccessRulesModified)
			{
				sectionsModified = AccessControlSections.Access;
			}
			if (AuditRulesModified)
			{
				sectionsModified |= AccessControlSections.Audit;
			}
			if (OwnerModified)
			{
				sectionsModified |= AccessControlSections.Owner;
			}
			if (GroupModified)
			{
				sectionsModified |= AccessControlSections.Group;
			}

			return sectionsModified;
		}

		private void ResetAccessControlSectionsModified()
		{
			AccessRulesModified = false;
			AuditRulesModified = false;
			OwnerModified = false;
			GroupModified = false;
		}

		public override Type AccessRightType { get { return typeof(int); } }
		public override Type AccessRuleType { get { return typeof(GenericAccessRule); } }
		public override Type AuditRuleType { get { return typeof(GenericAuditRule); } }
	}
}
