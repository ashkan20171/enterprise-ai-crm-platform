from pathlib import Path
s=Path("Controllers/CRMController.cs").read_text(encoding="utf-8")
a=s.index("public async Task<IActionResult> UpdateUser(")
b=s.index("public async Task<IActionResult> ResetUserPassword(",a)
method=s[a:b]
assert "roleManager.RoleExistsAsync(vm.Role)" in method
assert "db.Database.BeginTransactionAsync()" in method
assert method.index("BeginTransactionAsync()") < method.index("AddToRoleAsync(u,vm.Role)")
assert method.index("RemoveFromRolesAsync(u,obsolete)") < method.index("CommitAsync()")
assert "if(!stamp.Succeeded)" in method
assert method.index('WriteAudit("UserUpdate"') < method.index("CommitAsync()")
assert "ValidateAntiForgeryToken" in method
print("Stage 80 admin transaction structure checks passed")
