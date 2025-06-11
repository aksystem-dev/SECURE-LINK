# 🛠️ SecureLink – Instalační příručka

## ✅ Předpoklady

- Windows Server s IIS (včetně **ASP.NET Core Hosting Bundle**)
- .NET 8 SDK
- Microsoft SQL Server
- HTTPS certifikát (pokud API a WebApp běží na oddělených serverech)

## 📦 Komponenty systému

- **SecureLink API**  
  Backendová REST služba pro generování a validaci bezpečných odkazů.

- **SecureLink Blazor WebApp**  
  Webové uživatelské rozhraní pro potvrzení akce přes bezpečný odkaz.

## 🔧 Instalace databáze

1. Otevřete SQL Server Management Studio.
2. Spusťte SQL skript `DB_Create.sql` (SecureLink.API - Data - Scripts - DB_Create.sql), který vytvoří potřebné tabulky:
   - `SecureLinkSettings`, `ActionOptions`, `SecureLinkRequestsLog`, `Users`, ...
3. Ujistěte se, že máte správně nastavenou connection string v `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=SecureLink;User Id=sa;Password=heslo;"
}
```

## 🔧 Konfigurace aplikací (`appsettings.json`)

Základní konfigurace obou aplikací (API i WebApp):

```json
"BaseUrl": "https://securelink.vasedomena.cz",
"JWT": {
  "Key": "dlouhyBezpecnyKlic",
  "Issuer": "SecureLink",
  "Audience": "SecureLinkUsers"
},
"Serilog": {
  "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
  "MinimumLevel": "Information",
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "File",
      "Args": {
        "path": "logs/log-.txt",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 10
      }
    }
  ]
},
"AuthSettings": {
  "Salt": "0rciVpEamINJuf9VEQYMlysLUqoVdDUlXneo",
  "Username": "ENC$...",
  "Password": "ENC$..."
},
"API": {
  "BaseUrl": "https://localhost:7052/"
}
```

Konfiguraci je možné nastavit dle potřeby a to tak, že otevřete soubor appsettings.json v libovolném textovém editoru.

Pro změnu connection string připojení najděte sekci "ConnectionStrings".

Do této sekce přidejte potřebný connection string. Například:

"ConnectionStrings": {
  "DefaultConnection": "Data Source=CZC0155-AUTOB\\POHODA_SQL;Initial Catalog=SecureLink;Integrated Security=False;User ID=SecureLinkLogin;Password=ENC$Qb9d3Bl9T7tcvJxdOlzkk+t02E59V2XsZJE453/MJ87Q1aaXM/EKRCvMydxlP9y1;Trust Server Certificate=True",
  "PohodaConnection": "Data Source=CZC0155-AUTOB\\POHODA_SQL;Initial Catalog=StwPh_17048052_2025;Integrated Security=False;User ID=SecureLinkLogin;Password=ENC$EZqqZKylC2WnKBRCJfnf/Pu9yHsHbuyFofnGicicrTWTf9PC79IAspIUPco2aVsX;Trust Server Certificate=True"
}

Tip: Pokud nechcete connection stringy přidávat ručně, můžete použít aplikaci ConfigEditor, který umožňuje jednoduše přidávat nové connection stringy, automaticky je zašifruje a umožní vám otestovat jejich správnost. 


## 🏗️ Build a publikace

```bash
dotnet publish SecureLink.Api -c Release -o ./publish-api
dotnet publish SecureLink.WebApp -c Release -o ./publish-web
```

Případně manuálně za pomocí Visual Studia. 


Nahrajte publikované soubory na server, např.:

- `C:\inetpub\SecureLinkApi`
- `C:\inetpub\SecureLinkWeb`

## 🌐 Nastavení IIS

1. Vytvořte dva weby v IIS (SecureLinkApi a SecureLinkWeb).
2. Pro oba weby nastavte Application Pool bez .NET CLR (No Managed Code).
3. Cesty směřujte na `publish-api` a `publish-web`.
4. V Blazor WebApp zkontrolujte `web.config`, že obsahuje:

```xml
<aspNetCore processPath="dotnet" arguments=".\SecureLink.WebApp.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```

### Scénář A – API a WebApp na stejném serveru

- V appsettings WebApp nastavte:

```json
"ApiBaseUrl": "http://localhost:5000"
```

### Scénář B – API a WebApp na oddělených serverech

- Použijte HTTPS certifikát a veřejnou doménu.
- Otevřete port 443 mezi servery.
- Nastavte ve WebApp:

```json
"ApiBaseUrl": "https://securelink-api.vasedomena.cz"
```

## 🧪 Ověření funkčnosti

- Otevřete `https://securelink.vasedomena.cz` pro uživatelské rozhraní.
- Vyzkoušejte API přes endpoint `/api/securelink/validate`.

Možné odpovědi:
- ✅ Odkaz platný
- ⚠️ Odkaz expiroval
- ❌ Odkaz neexistuje

## 🔄 Napojení na EmailSMSGate

V hlavním nastavení aplikace EmailSMSGate najdete nově přidané tlačítko „Nastavení SecureLink“.
Kliknutím na toto tlačítko se otevře formulář pro konfiguraci propojení se SecureLink API.

Na obrazovce vyplňte:
Uživatelské jméno (pokud zadáte nové jméno, uživatel se automaticky vytvoří)
Heslo uživatele
Jméno databáze (musí přesně odpovídat názvu v sekci ConnectionStrings, např. „PohodaConnection“)
API URL (adresa API získaná z nastavení IIS, např.: http://localhost:80/)

Následně je možné v nastavení pravidla přidat nastavení pro secureLink a generování odkazu.
Pozor licence pro EmailSMSGate musí mít jako součást SecureLink

## 🛡️ Bezpečnost a provoz

- Omez přístup k API pomocí IP whitelistu.
- Využijte JWT autorizaci.
- Aktivujte přesměrování HTTP → HTTPS.
- Sledujte Serilog logy v `logs/log-*.txt`.
- Uživatelé a přihlašovací údaje v `AuthSettings` jsou šifrovány.

## ⚙️ Použité technologie

| Technologie | Účel |
|-------------|------|
| **.NET 8** | Hlavní framework pro API i Blazor aplikaci |
| **Blazor Server** | UI pro potvrzovací stránku |
| **Dapper** | Přístup k databázi (Dapper pro výkonnost) |
| **Serilog** | Logování do souborů a konzole |
| **JWT** | Autorizace API požadavků |
| **IIS** | Hostování publikovaných aplikací |

---

