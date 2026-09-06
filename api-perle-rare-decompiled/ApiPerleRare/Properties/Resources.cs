using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace ApiPerleRare.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resources
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				ResourceManager temp = new ResourceManager("ApiPerleRare.Properties.Resources", typeof(Resources).Assembly);
				resourceMan = temp;
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static string CreateContactAnnonces => ResourceManager.GetString("CreateContactAnnonces", resourceCulture);

	internal static string CreateContactAnnoncesParameters => ResourceManager.GetString("CreateContactAnnoncesParameters", resourceCulture);

	internal static string MailTemplate => ResourceManager.GetString("MailTemplate", resourceCulture);

	internal static string NicolasQuery => ResourceManager.GetString("NicolasQuery", resourceCulture);

	internal static string NotificationMailTemplate => ResourceManager.GetString("NotificationMailTemplate", resourceCulture);

	internal Resources()
	{
	}
}
