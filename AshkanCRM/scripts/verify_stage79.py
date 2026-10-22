#!/usr/bin/env python3
"""Static regression checks for administrator mutation endpoints; not an integration test."""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
source=(root/'Controllers/CRMController.cs').read_text(encoding='utf-8')
checks={
 'create role validation':'!await roleManager.RoleExistsAsync(vm.Role)',
 'self-demotion protection':'(isSelf||isBootstrapAdmin)&&(!vm.IsActive||vm.Role!=Roles.Admin)',
 'role assignment checked':'if(!added.Succeeded)',
 'role removal checked':'if(!removed.Succeeded)',
 'account creation role checked':'if(!assigned.Succeeded)',
 'user mutation audit':'WriteAudit("UserUpdate"',
 'creation audit':'WriteAudit("UserCreate"',
 'security stamp refresh':'UpdateSecurityStampAsync(u)',
}
for name,marker in checks.items():
 assert marker in source,f'Missing: {name}'
for method in ('CreateUser','UpdateUser','ResetUserPassword','UnlockUser','SaveOrganizationSettings'):
 assert re.search(r'\[Authorize\(Roles=Roles.Admin\).*?HttpPost,ValidateAntiForgeryToken\]\s*public async Task<IActionResult> '+method+r'\(',source,re.S),f'Authorization/antiforgery missing for {method}'
print('Stage 79 administrator security structural checks passed.')
