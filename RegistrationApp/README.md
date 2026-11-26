# GCMAC 2027 Registration App — Deployment Guide

.NET 8 Razor Pages app. Data SQL Server se aati hai jab `ConnectionStrings:SqlServer` set hoti hai; agar connection string empty hai to app local JSON fallback (`App_Data/registrations.json`) use karta hai. Production me SQL Server hi use karein.

## 1. Server prerequisites

- Windows Server with IIS (recommended) OR any host that can run .NET 8 apps.
- **.NET 8 Hosting Bundle** installed on IIS server (includes ASP.NET Core runtime + ANCM).
- SQL Server (Express/Standard) with a database, e.g. `GcmacRegistration`.
- Public HTTPS origin, e.g. `https://registration.example.com` — PNB return URL aur email QR/pass links isi par bante hain.
- PNB payment gateway credentials (Request URL, Verify URL, Merchant Code, Key, IV).
- Email/SMS API credentials (notification worker QR/pass emails bhejta hai).

> Deploy **clean committed source** se karein. Local `appsettings.json` me apni machine ka DB connection string/admin password ho sakta hai — publish se pehle placeholders rakhein ya environment vars se override karein.

## 2. Build/publish

Local clean checkout par:

```bat
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o C:\inetpub\GcmacRegistration
```

IIS nahi use kar rahe ho to self-host:

```bat
set ASPNETCORE_URLS=http://0.0.0.0:5080
dotnet C:\inetpub\GcmacRegistration\RegistrationApp.dll
```

Is case me reverse proxy/TLS terminate karein aur niche diye gaye `PublicBaseUrl` env vars zaroor set karein.

## 3. SQL Server setup

App startup par khud `Database.MigrateAsync()` chalata hai jab connection string milti hai. Pehle run ke liye DB user ko `db_datareader`, `db_datawriter`, `db_ddladmin` chahiye. Migration ke baad `db_ddladmin` hata bhi sakte hain.

SQL authentication connection string:

```text
Server=SERVERNAME;Database=GcmacRegistration;User Id=gcmac_app;Password=STRONG_PASSWORD;TrustServerCertificate=True;
```

Windows auth (IIS app pool identity):

```text
Server=SERVERNAME;Database=GcmacRegistration;Trusted_Connection=True;TrustServerCertificate=True;
```

Environment variable:

```text
ConnectionStrings__SqlServer=Server=SERVERNAME;Database=GcmacRegistration;User Id=gcmac_app;Password=STRONG_PASSWORD;TrustServerCertificate=True;
```

> Windows auth use kar rahe ho to IIS app pool identity (e.g. `IIS APPPOOL\GcmacRegistration`) ko SQL Server login + DB permissions do.

Manual migration alternative:

```bat
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef database update --connection "Server=...;Database=GcmacRegistration;..."
```

## 4. IIS setup

1. `.NET 8 Hosting Bundle` install karein, phir `iisreset`.
2. New Application Pool: name `GcmacRegistration`, **.NET CLR version = No managed code**.
3. New Site: physical path `C:\inetpub\GcmacRegistration`, HTTPS binding with certificate.
4. App pool identity ko publish folder par read/execute permission do. `App_Data` par write permission tab isliye do agar kisi din JSON fallback use ho jaye.
5. Environment variables set karein (system-level ya `web.config` me), phir app pool recycle.
6. `UseHttpsRedirection()` enabled hai — HTTPS binding nahi hai to HTTP requests HTTPS par redirect hokar fail honge.

## 5. Required environment variables

Environment variable me nested config keys `__` se likhi jaati hain. Values kisi source control me na rakhein.

### Core

| Variable | Example | Purpose |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Production mode |
| `ConnectionStrings__SqlServer` | `Server=...;Database=GcmacRegistration;...` | SQL Server source of truth |
| `Admin__Email` | `admin@icmai.in` | Admin login email |
| `Admin__Password` | `<strong-password>` | Admin login password (env me rakhein) |

### Public URLs

| Variable | Example | Purpose |
|---|---|---|
| `Payment__PublicBaseUrl` | `https://registration.example.com` | PNB ko bheja jane wala `returnURL` |
| `Notifications__PublicBaseUrl` | `https://registration.example.com` | Email me QR/pass/receipt absolute links |

Dono me trailing slash na rakhein. PNB se return `https://registration.example.com/Register/PaymentReturn` par aana chahiye.

### Registration shortcut

`/registration` seedha delegate sign-in pane kholta hai aur successful login ke baad `/Register/MyRegistrations` par bhejta hai. Header aur footer ka **Registration** link ab isi URL par hai.

### PNB payment gateway

| Variable | Example |
|---|---|
| `Payment__Provider` | `PNB` (default already PNB hai) |
| `Payment__Pnb__RequestUrl` | `<institute-supplied request URL>` |
| `Payment__Pnb__VerifyUrl` | `<institute-supplied verify URL>` |
| `Payment__Pnb__MerchantCode` | `<merchant code>` |
| `Payment__Pnb__Key` | `<key>` |
| `Payment__Pnb__Iv` | `<iv>` |
| `Payment__Pnb__BankCode` | `470` (default) |

Config missing par app startup me warning deta hai. Pay button PNB se miley URL par `302` redirect karta hai; callback `/Register/PaymentReturn` par verify hota hai.

### Member OTP login

Member login ek hi OTP use karta hai — user **Email** ya **SMS** me se koi bhi channel select karke validate kar sakta hai. OTP dono channels par same hota hai (`MemberLogin__DemoSecret`, default `123456`).

| Variable | Example | Purpose |
|---|---|---|
| `MemberLogin__DemoSecret` | `123456` | Demo OTP (production me change karein) |
| `MemberLogin__OtpValidityMinutes` | `10` | SMS/email template me bataya jane wala validity text |

Signup ke OTP bhi ab queue me jaate hain (email/SMS API configured hone par actual send hote hain). Invalid OTP par `otp_failed` template bhi queue hota hai.

### Member API

| Variable | Example | Purpose |
|---|---|---|
| `MemberApi__BaseUrl` | `https://eicmai.in/member_gcmac_api/` | ICMAI member verification |
| `MemberApi__ApiKey` | `<api key>` | API key |
| `MemberApi__RequireVerification` | `true` | Verification enforce karna |
| `MemberApi__TimeoutSeconds` | `10` | Timeout |

### Notifications (email + SMS)

| Variable | Example |
|---|---|
| `Notifications__AdminEmail` | `gcmac2027@icmai.in` |
| `Notifications__AdminMobile` | `<admin mobile>` |
| `Notifications__MaxDelegatesPerMember` | `5` |
| `Notifications__MaxAdditionalDelegatesPerMember` | `5` |
| `Notifications__Email__ApiUrl` | `<email API URL>` |
| `Notifications__Email__ApiKey` | `<email API key>` |
| `Notifications__Email__FromEmail` | `gcmac2027@icmai.in` |
| `Notifications__Email__FromName` | `GCMAC 2027` |
| `Notifications__Sms__ApiUrl` | `<SMS API URL>` |
| `Notifications__Sms__ApiKey` | `<SMS API key>` |
| `Notifications__Sms__SenderId` | `<sender id>` |

SMS provider template IDs (agar provider ko chahiye). Neeche wale Airtel/DLT IDs code me default hain; env var se override ho sakte hain:

```text
Notifications__Sms__TemplateIds__login_otp=1077157310009169030
Notifications__Sms__TemplateIds__account_created=1077312930004515505
Notifications__Sms__TemplateIds__registration_completed=1077490120005707242
Notifications__Sms__TemplateIds__login_success=1077127730008187762
Notifications__Sms__TemplateIds__otp_failed=1077431160005669032
Notifications__Sms__TemplateIds__registration_submitted=<id>
Notifications__Sms__TemplateIds__payment_success=<id>
Notifications__Sms__TemplateIds__offline_submitted=<id>
Notifications__Sms__TemplateIds__payment_failed=<id>
Notifications__Sms__TemplateIds__offline_verified=<id>
Notifications__Sms__TemplateIds__admin_offline_proof=<id>
```

Templates `Templates-GCMAC2027-AIRTEL(1).xlsx` se add kiye gaye hain: `login_otp`, `account_created`, `registration_completed`, `login_success`, `otp_failed`. Payment confirmed par SMS ke liye `registration_completed` use hota hai; email me QR/pass wala detailed message rehta hai.

### Notification forwarding (OTP/email mirroring)

Kisi bhi notification (OTP ya kisi template) ki copy apne email par forward karne ke liye:

| Variable | Example | Purpose |
|---|---|---|
| `Notifications__Forwarding__Enabled` | `true` | Forwarding on/off |
| `Notifications__Forwarding__Email` | `you@example.com` | Copy yahan bheji jayegi |
| `Notifications__Forwarding__Templates` | `login_otp,otp_failed` | Comma-separated template keys; khali chhodein to sab templates forward |

Forwarded copy email channel par queue hoti hai, chahe original SMS ho ya email. Body me `Forwarded sms for <recipient>:` ya `Forwarded email for <recipient>:` prefix juda hota hai. OTP wali copy me bhi wahi OTP rahega.

Email API ko JSON me `apiKey`, `from`, `to`, `subject`, `text`, `html` fields milte hain. `html` me inline QR image hoti hai.

## 6. Data flow / tables

- `Registrations` — member + delegates, `MembershipId = <member number>-<delegate name>`.
- `PaymentTransactions` — single combined payment batch rows.
- `Notifications` — email/SMS queue; worker retry karta hai.
- `MemberAccounts`, `Accounts` — member/admin accounts.

Offline payment verify admin panel se:

```text
https://registration.example.com/Admin
```

## 7. Post-deploy checks

1. `https://registration.example.com/` home page load.
2. Member flow: `/Account/SignIn?type=member&returnUrl=/Member/Registration`.
3. Registration save → `/Register/BatchPayment` → **Pay** → PNB gateway par redirect.
4. Payment return ke baad registrations `Paid` aur email me QR/pass link.
5. SQL me rows check:

```sql
SELECT Id, FullName, MembershipId, PaymentBatchId, PaymentStatus FROM Registrations;
SELECT MerchantTxnRefNo, BatchId, Status, Amount FROM PaymentTransactions;
SELECT Id, Recipient, TemplateKey, Status FROM Notifications;
```

6. Firewall me 443 (aur reverse proxy use kar rahe ho to 80) open.

## 8. Common issues

- **PNB configured nahi** — env vars check karein; startup warning dekhein.
- **Callback fail** — `Payment__PublicBaseUrl` public HTTPS hona chahiye; PNB dashboard me same return URL whitelisted honi chahiye.
- **Email QR links localhost dikha rahe hain** — `Notifications__PublicBaseUrl` set karein.
- **JSON data dikh raha hai / SQL data nahi** — `ConnectionStrings__SqlServer` empty ya galat hai; `App_Data/registrations.json` fallback chal raha hai.
- **IIS 500.30/500.19** — .NET 8 Hosting Bundle missing ya app pool managed code mode galat.
- **DB permissions error** — migration ke liye app user ko `db_ddladmin` do ya migration manual chalayein.

## 9. Local development note

Local machine par `appsettings.json` me DB connection string rakhi ja sakti hai, lekin use commit ya publish me na le jayein. Demo OTP `MemberLogin__DemoSecret` default `123456` hai — production me change karein ya member API verification use karein.
