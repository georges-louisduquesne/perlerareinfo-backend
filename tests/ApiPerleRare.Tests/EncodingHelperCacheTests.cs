using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using ApiPerleRare.Helpers;
using Xunit;

namespace ApiPerleRare.Tests;

public class EncodingHelperCacheTests
{
	[Fact]
	public void GetStringProperties_survives_concurrent_cache_misses()
	{
		Type[] types = CreateFreshStringTypes(48);
		ConcurrentBag<Exception> errors = new ConcurrentBag<Exception>();
		Parallel.For(0, 32, _ =>
		{
			try
			{
				for (int round = 0; round < 8; round++)
				{
					foreach (Type type in types)
					{
						PropertyInfo[] props = EncodingHelper.GetStringProperties(type);
						if (props.Length != 1 || props[0].Name != "Nom")
						{
							throw new InvalidOperationException("cache incoherent pour " + type.Name);
						}
					}
				}
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}
		});
		Assert.Empty(errors);
	}

	private static Type[] CreateFreshStringTypes(int count)
	{
		AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName("EncodingHelperCacheRace_" + Guid.NewGuid().ToString("N")),
			AssemblyBuilderAccess.Run);
		ModuleBuilder module = assembly.DefineDynamicModule("m");
		Type[] types = new Type[count];
		for (int i = 0; i < count; i++)
		{
			TypeBuilder typeBuilder = module.DefineType("T" + i, TypeAttributes.Public);
			FieldBuilder field = typeBuilder.DefineField("_nom", typeof(string), FieldAttributes.Private);
			PropertyBuilder prop = typeBuilder.DefineProperty("Nom", PropertyAttributes.None, typeof(string), null);
			MethodBuilder getter = typeBuilder.DefineMethod(
				"get_Nom",
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
				typeof(string),
				Type.EmptyTypes);
			ILGenerator getIl = getter.GetILGenerator();
			getIl.Emit(OpCodes.Ldarg_0);
			getIl.Emit(OpCodes.Ldfld, field);
			getIl.Emit(OpCodes.Ret);
			MethodBuilder setter = typeBuilder.DefineMethod(
				"set_Nom",
				MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
				null,
				new[] { typeof(string) });
			ILGenerator setIl = setter.GetILGenerator();
			setIl.Emit(OpCodes.Ldarg_0);
			setIl.Emit(OpCodes.Ldarg_1);
			setIl.Emit(OpCodes.Stfld, field);
			setIl.Emit(OpCodes.Ret);
			prop.SetGetMethod(getter);
			prop.SetSetMethod(setter);
			types[i] = typeBuilder.CreateType();
		}
		return types;
	}
}
