using Microsoft.AspNetCore.Authorization;

namespace RS.Fahrzeugsystem.Api.Authorization
{
	public class HasPermissionAttribute : AuthorizeAttribute
	{
		public HasPermissionAttribute(string permission)
		{
			Policy = permission;
		}
	}
}