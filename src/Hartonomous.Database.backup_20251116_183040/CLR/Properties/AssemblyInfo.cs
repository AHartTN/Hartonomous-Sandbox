using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SqlClrFunctions")]
[assembly: AssemblyDescription("SQL CLR functions for Hartonomous AI inference engine")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Hartonomous")]
[assembly: AssemblyCopyright("Copyright © 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a1234567-abcd-ef12-3456-567890abcdef")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// SQL Server CLR Permission Set - MUST match CREATE ASSEMBLY ... WITH PERMISSION_SET
// CRITICAL: This attribute is REQUIRED when using CLR strict security (SQL 2017+)
// Without this, DACPAC deployment fails with:
// "CREATE ASSEMBLY failed because assembly was compiled with /UNSAFE option,
//  but the assembly was not registered with the required PERMISSION_SET = UNSAFE option."
[assembly: PermissionSet(SecurityAction.RequestMinimum, Unrestricted = true)]
