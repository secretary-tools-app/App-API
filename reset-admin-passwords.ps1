param(
    [string]$BaseUrl = "https://atas-app-api-production.up.railway.app",
    [string]$AdminSecret = $env:Admin__Secret
)

if ([string]::IsNullOrWhiteSpace($AdminSecret)) {
    Write-Error "Informe o segredo admin via -AdminSecret ou pela variável de ambiente Admin__Secret."
    exit 1
}

$users = @{
    "criciuma1_bispo" = "Criciuma1.1@2033"
    "criciuma1_conselheiro_1" = "Criciuma1.2@2033"
    "criciuma1_conselheiro_2" = "Criciuma1.3@2033"
    "criciuma1_secretario_1" = "Criciuma1.4@2033"
    "criciuma1_secretario_2" = "Criciuma1.5@2033"
    "criciuma1_secretario_3" = "Criciuma1.6@2033"
    "criciuma2_bispo" = "Criciuma2.1@2088"
    "criciuma2_conselheiro_1" = "Criciuma2.2@2088"
    "criciuma2_conselheiro_2" = "Criciuma2.3@2088"
    "criciuma2_secretario_1" = "Criciuma2.4@2088"
    "criciuma2_secretario_2" = "Criciuma2.5@2088"
    "criciuma2_secretario_3" = "Criciuma2.6@2088"
    "criciuma3_bispo" = "Criciuma3.1@2066"
    "criciuma3_conselheiro_1" = "Criciuma3.2@2066"
    "criciuma3_conselheiro_2" = "Criciuma3.3@2066"
    "criciuma3_secretario_1" = "Criciuma3.4@2066"
    "criciuma3_secretario_2" = "Criciuma3.5@2066"
    "criciuma3_secretario_3" = "Criciuma3.6@2066"
    "icara_bispo" = "Icara.1@2099"
    "icara_conselheiro_1" = "Icara.2@2099"
    "icara_conselheiro_2" = "Icara.3@2099"
    "icara_secretario_1" = "Icara.4@2099"
    "icara_secretario_2" = "Icara.5@2099"
    "icara_secretario_3" = "Icara.6@2099"
    "ararangua_bispo" = "Ararangua.1@2010"
    "ararangua_conselheiro_1" = "Ararangua.2@2010"
    "ararangua_conselheiro_2" = "Ararangua.3@2010"
    "ararangua_secretario_1" = "Ararangua.4@2010"
    "ararangua_secretario_2" = "Ararangua.5@2010"
    "ararangua_secretario_3" = "Ararangua.6@2010"
}

$uri = "$BaseUrl/api/admin/users/password"

foreach ($entry in $users.GetEnumerator()) {
    $body = @{ username = $entry.Key; newPassword = $entry.Value } | ConvertTo-Json -Compress

    try {
        $response = Invoke-WebRequest -Uri $uri -Method Put -Headers @{ "X-Admin-Secret" = $AdminSecret } -Body $body -ContentType "application/json" -UseBasicParsing
        Write-Host ("[OK] {0} -> {1}" -f $entry.Key, $response.StatusCode)
    }
    catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        Write-Host ("[FAIL] {0} -> {1}" -f $entry.Key, $status)
    }

    Start-Sleep -Milliseconds 500
}
