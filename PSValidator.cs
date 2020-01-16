namespace Boden.Powershell
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Management.Automation.Language;

	/// <summary>
	/// Provides enumerated values to determine how a restriction items are treated.
	/// </summary>
	public enum RestrictionMode
	{
		/// <summary>
		/// Items not contained in the list or defined in the script are not permitted.
		/// </summary>
		Whitelist,

		/// <summary>
		/// Items contained in list are not permitted.
		/// </summary>
		Blacklist
	}

	public class PSValidator
	{
		public bool IsValid { get; private set; } = false;

		public RestrictionMode CommandRestrictionMode { get; set; } = RestrictionMode.Whitelist;
		public List<string> RestrictionCommands { get; } = new List<string>();

		public RestrictionMode TypeRestrictionMode { get; set; } = RestrictionMode.Whitelist;
		public List<Type> RestrictionTypes { get; } = new List<Type>();
		public bool AlwaysAllowBasicTypes { get; set; } = true;

		public RestrictionMode AutomaticVariableRestrictionMode { get; set; } = RestrictionMode.Blacklist;
		public List<string> RestrictionAutomaticVariables { get; } = new List<string>();

		public RestrictionMode DriveRestrictionMode { get; set; } = RestrictionMode.Whitelist;
		public List<string> RestrictionDrives { get; } = new List<string>();

		public bool AllowFileRedirection { get; set; } = false;
		public bool AllowStreamRedirection { get; set; } = false;

        public Ast Ast { get; private set; }
		public IEnumerable<Ast> InvalidPieces { get; private set; }
        public IEnumerable<ParseError> ParseErrors { get; private set; }
        public IEnumerable<Token> Tokens { get; private set; }

		private List<string> ScriptFunctions { get; set; }

		public void Validate(string script)
		{
            ScriptBlockAst scriptAst = Parser.ParseInput(scriptText, out Token[] scriptTokens, out ParseError[] scriptParseErrors);
            
            Ast = scriptAst;
            ParseErrors = scriptParseErrors.ToList();
            Tokens = scriptTokens.ToList();
            
			Validate(ast);
		}

		public void Validate(Ast scriptAst)
		{
			IEnumerable<Ast> allAst = scriptAst.FindAll(c => c != null, true);

			// if commands are running a whitelist, we need to track functions defined in script
			if (CommandRestrictionMode == RestrictionMode.Whitelist)
			{
				ScriptFunctions = (from a in allAst
								   where a is FunctionDefinitionAst
								   select (a as FunctionDefinitionAst).Name).ToList();
			}

			List<Ast> invalid = new List<Ast>();

			foreach (Ast curAst in allAst)
			{
				switch (curAst)
				{
					case CommandAst cmd when !IsValidCommand(cmd):
						invalid.Add(cmd);
						break;

					case ConvertExpressionAst convertExp when !IsValidType(convertExp):
						invalid.Add(convertExp);
						break;

					case TypeExpressionAst typeExp when !IsValidType(typeExp):
						invalid.Add(typeExp);
						break;

					case VariableExpressionAst variable when !IsValidDriveQualifiedVariable(variable) ||
					                                         !IsValidAutomaticVariable(variable):
						invalid.Add(variable);
						break;

					case MergingRedirectionAst merging when !AllowStreamRedirection:
						invalid.Add(merging);
						break;

					case FileRedirectionAst redirect when !AllowFileRedirection:
						invalid.Add(redirect);
						break;
				}
			}

			InvalidPieces = invalid;

			IsValid = InvalidPieces.Count() == 0;
		}

		private bool IsValidAutomaticVariable(VariableExpressionAst varAst)
		{
			VariableToken varTok = GetToken<VariableToken>(varAst);

			if (varTok == null)
			{
				return true;
			}

			if (PSInfo.AutomaticVariables.Contains(varTok.Name))
			{
				bool inRestrictionList = RestrictionAutomaticVariables.Contains(varTok.Name, StringComparer.OrdinalIgnoreCase);

				switch (AutomaticVariableRestrictionMode)
				{
					case RestrictionMode.Whitelist:
						return inRestrictionList;

					case RestrictionMode.Blacklist:
						return !inRestrictionList;
				}
			}

			return true;
		}

		private bool IsValidCommand(CommandAst cmdAst)
		{
			if (cmdAst.InvocationOperator != TokenKind.Unknown)
			{
				return false;
			}

			if (cmdAst.CommandElements[0] is StringConstantExpressionAst cmdExp && cmdExp.StringConstantType == StringConstantType.BareWord)
			{
				bool inRestrictionList = (bool)RestrictionCommands?.Contains(cmdExp.Value, StringComparer.OrdinalIgnoreCase);

				switch (CommandRestrictionMode)
				{
					case RestrictionMode.Whitelist:
						// check our script funcs if there are any and the command is not in the restriction list
						if (ScriptFunctions != null && !inRestrictionList)
						{
							inRestrictionList = ScriptFunctions.Contains(cmdExp.Value, StringComparer.OrdinalIgnoreCase);
						}

						return inRestrictionList;

					case RestrictionMode.Blacklist:
						return !inRestrictionList;
				}
			}

			return false;
		}

		private bool IsValidDriveQualifiedVariable(VariableExpressionAst varAst)
		{
			VariableToken varTok = GetToken<VariableToken>(varAst);

			if (varTok == null)
			{
				return true;
			}

			if (varTok.VariablePath.IsDriveQualified)
			{
				bool inRestrictionList = (bool)RestrictionDrives?.Contains(varTok.VariablePath.DriveName);

				switch(DriveRestrictionMode)
				{
					case RestrictionMode.Whitelist:
						return inRestrictionList;

					case RestrictionMode.Blacklist:
						return !inRestrictionList;
				}
			}
			
			return true;
		}

		private bool IsValidType(Type t)
		{
			bool isBasicType = PSInfo.IsBasicType(t);

			// if it is a basic type and basic types are always allowed, it doesnt matter
			// what the restriction mode/list is
			if (isBasicType && AlwaysAllowBasicTypes)
			{
				return true;
			}
			else
			{
				bool inRestrictionList = RestrictionTypes.Contains(t);

				switch (TypeRestrictionMode)
				{
					case RestrictionMode.Whitelist:
						return inRestrictionList;

					case RestrictionMode.Blacklist:
						return !inRestrictionList;
				}
			}

			return false;
		}

		private bool IsValidType(TypeExpressionAst typeAst)
		{
			Type reflectType = typeAst.TypeName.GetReflectionType();

			return IsValidType(reflectType);
		}

		private bool IsValidType(ConvertExpressionAst convertAst)
		{
			Type reflectType = convertAst.Type.TypeName.GetReflectionType();

			return IsValidType(reflectType);
		}

		private bool IsValidType(InvokeMemberExpressionAst invokeAst)
		{
			var typeExp = invokeAst.Find(e => e is TypeExpressionAst, true) as TypeExpressionAst;

			Type reflectType = typeExp.TypeName.GetReflectionType();

			return IsValidType(reflectType);
		}

		public static T GetToken<T>(Ast ast)
		{
			Parser.ParseInput(ast.Extent.Text, out Token[] tok, out _);

			T foundTok = tok.Where(t => t is T).Cast<T>().FirstOrDefault();

			return foundTok;
		}
	}
}
