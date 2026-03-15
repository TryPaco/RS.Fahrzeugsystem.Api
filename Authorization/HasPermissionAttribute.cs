using Microsoft.AspNetCore.Authorization;

namespace RS.Fahrzeugsystem.Api.Authorization
{
	public sealed class HasPermissionAttribute : AuthorizeAttribute
	{
		public HasPermissionAttribute(string permission)
		{
			Policy = $"{PermissionPolicyProvider.PolicyPrefix}{permission}";
		}
	}
}