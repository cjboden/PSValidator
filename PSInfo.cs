namespace Boden.Powershell
{
	using System;

	public static class PSInfo
    {
		public static readonly Type[] BasicTypes = new Type[]
		{
			// using fully qualified names for explicitness
			typeof(System.Boolean),
			typeof(System.Byte),
			typeof(System.SByte),
			typeof(System.Char),
			typeof(System.Decimal),
			typeof(System.Double),
			typeof(System.Single),
			typeof(System.Int16),
			typeof(System.UInt16),
			typeof(System.Int32),
			typeof(System.UInt32),
			typeof(System.Int64),
			typeof(System.UInt64),
			typeof(System.Object),
			typeof(System.String),
			typeof(System.DateTime),
			typeof(System.Array),
			typeof(System.Collections.Hashtable),
			typeof(System.Management.Automation.SwitchParameter)
		};

		public static bool IsBasicType(Type t)
		{
			foreach (Type bType in BasicTypes)
			{
				if (bType == t)
					return true;
			}

			return false;
		}

		// some autovars such as $null, $true, and $false are not in this list
		// because for validation purposes they are considered to be lingual constructs
		// this means that they will be validated simply as variables
		public static readonly string[] AutomaticVariables = new string[]
		{
			"$",
			"?",
			"^",
			"ConsoleFileName",
			"Error",
			"Event",
			"EventArgs",
			"EventSubscriber",
			"ExecutionContext",
			"HOME",
			"Host",
			"input",
			"LastExitCode",
			"MyInvocation",
			"NestedPromptLevel",
			"PID",
			"PROFILE",
			"PSBoundParameters",
			"PSCmdlet",
			"PSCommandPath",
			"PSCulture",
			"PSDebugContext",
			"PSHOME",
			"PSScriptRoot",
			"PSSenderInfo",
			"PSUICulture",
			"PSVersionTable",
			"PWD",
			"Sender",
			"ShellId",
			"StackTrace"
		};

		public static bool IsNumericType(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;

				default:
					return false;
			}
		}
	}
}
