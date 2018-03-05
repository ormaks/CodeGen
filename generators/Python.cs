using System.Collections.Generic;
using System.Linq;

namespace CodeGen.generators
{
	/// <inheritdoc />
	/// <summary>
	/// Python language generator
	/// </summary>
	public class PythonGenerator : Generator
	{
		private const string ClassFormat = "class {0}{1}:\n{2}{3}{4}";
		private string Indent { get; set; } = GeneratorConf.GetIndent(true, 4);
		
		/// <inheritdoc />
		public override Dictionary<string, string> Generate(Package pkg)
		{
			var data = new Dictionary<string, string>();
			Indent = GeneratorConf.GetIndent(!pkg.UseSpaces, 4);
			foreach (var @class in pkg.Classes)
			{
				data[@class.Name] = GenerateClass(@class) + "\n";
			}

			return data;
		}

		/// <inheritdoc />
		protected override string GenerateClass(Class @class)
		{
			string fields = "", inherits = "", methods = "", classes = "";

			if (@class.Parent != "")
			{
				inherits = "(" + @class.Parent + ")";
			}

			if (@class.Fields?.Length > 0)
			{
				fields = GeneratorConf.ShiftCode(GenerateInit(@class), 1, Indent);
			}
			
			methods = @class.Methods?.Aggregate("\n" + methods,
				(current, method) => current + GeneratorConf.ShiftCode(GenerateMethod(method), 1, Indent) + "\n");
			
			classes = @class.Classes?.Aggregate("\n" + classes,
				(current, cls) => current + GeneratorConf.ShiftCode(GenerateClass(cls), 1, Indent));
			
			var result = string.Format(ClassFormat, @class.Name, inherits, fields, methods, classes);
			
			if (result[result.Length - 2] == ':' && result[result.Length - 1] == '\n')
			{
				result += Indent + "pass";
			}

			return result;
		}

		/// <inheritdoc />
		protected override string GenerateField(Field field)
		{
			var result = Indent;

			if (field.Access == "public")
			{
				field.Name = field.Name?.First().ToString().ToUpper() + field.Name?.Substring(1);
			}

			result += field.Name + " " + field.Type;

			return result;
		}

		/// <inheritdoc />
		protected override string GenerateMethod(Method method)
		{
			return GenerateMethodWithBody(method, "pass");
		}
		
		
		private string GenerateInit(Class @class)
		{
			string result = "", body = "";
			var init = new Method
			{
				Name = "__init__",
				Parameters = new Parameter[] { }
			};
			foreach (var field in @class.Fields)
			{
				init.Parameters.Append(new Parameter()
				{
					Name = field.Name,
					Default = field.Default
				});
				body += "self." + field.Name + " = " + field.Name + "\n";
			}

			result += GenerateMethodWithBody(init, body);

			return result;
		}
		

		private string GenerateMethodWithBody(Method method, string body) 
		{
			var result = "def ";

			switch (method.Access)
			{
				case "private":
					method.Name = "__" + method.Name;
					break;
				case "protected":
					method.Name = "_" + method.Name;
					break;
			}

			result += method.Name + "(";

			if (method.Static)
			{
				result = "@staticmethod\n" + result;
			} 
			else
			{
				result += "self";
				if (method.Parameters?.Length > 0)
				{
					result += ", ";
				}
			}

			for (var i = 0; i < method.Parameters?.Length; i++)
			{
				result += method.Parameters[i].Name;
				if (method.Parameters[i].Default != "")
				{
					result += "=" + method.Parameters[i].Default;
				}
				if (i + 1 < method.Parameters?.Length)
				{
					result += ", ";
				}
			}

			if (body == "pass")
			{
				body += '\n';
			}
			
			result += "):\n" + GeneratorConf.ShiftCode(body, 1, Indent);
			return result;
		}
	}
}

