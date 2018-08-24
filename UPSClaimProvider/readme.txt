$trustedTokenIssuer = Get-SPTrustedIdentityTokenIssuer;
$trustedTokenIssuer;


$trustedTokenIssuer.ClaimProviderName = "UPSClaimProvider";
$trustedTokenIssuer.Update();
