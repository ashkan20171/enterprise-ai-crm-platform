from pathlib import Path
s=Path("Controllers/CRMController.cs").read_text()
for required in ["ResetUserPassword(string id,string newPassword)","UnlockUser(string id)","ResetPasswordAsync(u,token,newPassword)","UpdateSecurityStampAsync(u)","SetLockoutEndDateAsync(u,null)","ResetAccessFailedCountAsync(u)","UserPasswordReset","UserUnlock", "if(!unlock.Succeeded)","if(!reset.Succeeded)"]:
    assert required in s, f"Missing safeguard: {required}"
assert "no secret logged" in s
print("Stage 81 administrator recovery structural checks passed")
