# Pro Module Licensing Strategy

**Last Updated**: October 24, 2025  
**Status**: Planning Phase  
**First Implementation**: Multi-Tenancy Module (Dec 2025)

---

## 🎯 Overview

NetMX uses a **source code copy + license key** model for Pro modules, NOT NuGet packages. This gives customers full access to source code while protecting our intellectual property through licensing.

---

## 🔑 Core Principles

### 1. Source Code Distribution
✅ **Pro modules distributed as source code** (like free modules)  
✅ Customers own the code and can customize it  
✅ No "black box" DLLs or obfuscation  
✅ Full debugging and modification capabilities  

### 2. License Key Protection
✅ License key required to run Pro module code  
✅ Validation happens at application startup  
✅ Graceful degradation if license invalid  
✅ Clear error messages for license issues  

### 3. One-Time Purchase Model
✅ Pay once, use forever (not subscription)  
✅ Includes 1 year of updates  
✅ Optional renewal for continued updates ($49/year)  
✅ Perpetual license (never expires)  

---

## 📦 Distribution Strategy

### Free Modules vs Pro Modules

| Aspect | Free Modules | Pro Modules |
|--------|--------------|-------------|
| **Distribution** | `netmx add module Identity` | `netmx add module MultiTenancy --license KEY` |
| **Source Code** | ✅ Full access | ✅ Full access |
| **License Check** | ❌ None | ✅ Required at runtime |
| **Cost** | FREE (MIT) | $149-$299 one-time |
| **Updates** | FREE forever | 1 year included, then $49/year |
| **Customization** | ✅ Unlimited | ✅ Unlimited |
| **Redistribution** | ✅ MIT license | ❌ Licensed per project |

---

## 🛠️ Technical Implementation

### Architecture

```
Pro Module Structure:
modules/MultiTenancy/              # Same structure as free modules
├── MultiTenancy.Core/
│   ├── Entities/
│   ├── LicenseValidation/         # ⭐ NEW: License validation logic
│   │   ├── ILicenseValidator.cs
│   │   ├── LicenseValidator.cs
│   │   ├── LicenseInfo.cs
│   │   └── LicenseException.cs
│   └── ...
├── MultiTenancy.Application/
│   └── ...
├── MultiTenancy.Web/
│   └── ...
└── module.json                    # Contains "isPro": true
```

### License Validation Flow

```
Application Startup
    ↓
Read License Key from appsettings.json
    ↓
Validate License Key (ILicenseValidator)
    ↓
    ├─ VALID → Register Pro module services
    └─ INVALID → Throw LicenseException with clear message
```

### Code Example

**module.json** (Pro Module Indicator):
```json
{
  "name": "MultiTenancy",
  "version": "1.0.0",
  "isPro": true,                    // ⭐ Marks as Pro module
  "licenseRequired": true,
  "pricing": {
    "oneTime": 299,
    "renewal": 49
  }
}
```

**appsettings.json** (Customer Configuration):
```json
{
  "NetMX": {
    "Licensing": {
      "MultiTenancy": {
        "LicenseKey": "NETMX-MT-ABC123-XYZ789-...",
        "LicensedTo": "Acme Corp",
        "ExpiresAt": "2026-12-31"     // For update eligibility
      }
    }
  }
}
```

**LicenseValidator.cs** (Validation Logic):
```csharp
public class LicenseValidator : ILicenseValidator
{
    private const string PublicKey = "..."; // RSA public key
    
    public LicenseInfo Validate(string licenseKey)
    {
        // 1. Decode license key (base64)
        // 2. Verify signature (RSA)
        // 3. Check expiration (for updates, not usage)
        // 4. Return LicenseInfo
        
        var decoded = DecodeAndVerify(licenseKey);
        
        return new LicenseInfo
        {
            IsValid = true,
            LicensedTo = decoded.CompanyName,
            ExpiresAt = decoded.UpdatesExpireAt,
            ModuleName = "MultiTenancy"
        };
    }
}
```

**ServiceCollectionExtensions.cs** (Registration with Validation):
```csharp
public static IServiceCollection AddMultiTenancy(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // 1. Get license key from config
    var licenseKey = configuration["NetMX:Licensing:MultiTenancy:LicenseKey"];
    
    if (string.IsNullOrEmpty(licenseKey))
    {
        throw new LicenseException(
            "MultiTenancy license key is missing. " +
            "Add 'NetMX:Licensing:MultiTenancy:LicenseKey' to appsettings.json. " +
            "Purchase a license at https://netmx.dev/pricing"
        );
    }
    
    // 2. Validate license
    var validator = new LicenseValidator();
    var licenseInfo = validator.Validate(licenseKey);
    
    if (!licenseInfo.IsValid)
    {
        throw new LicenseException(
            $"Invalid MultiTenancy license key. " +
            $"Reason: {licenseInfo.ValidationError}. " +
            $"Contact support@netmx.dev"
        );
    }
    
    // 3. Log license info (for transparency)
    services.AddSingleton(licenseInfo);
    
    // 4. Register module services (only if license valid)
    services.AddScoped<ITenantManager, TenantManager>();
    services.AddScoped<ITenantResolver, TenantResolver>();
    // ... rest of services
    
    return services;
}
```

---

## 🔐 License Key Format

### Structure

```
NETMX-{MODULE}-{SIGNATURE}-{PAYLOAD}

Example:
NETMX-MT-8A3F5D-eyJjb21wYW55IjoiQWNtZSBDb3JwIiwiZXhwaXJlcyI6IjIwMjYtMTItMzEifQ

Components:
- NETMX: Prefix
- MT: Module code (MultiTenancy)
- 8A3F5D: Signature (RSA signature of payload)
- eyJ...: Base64-encoded JSON payload
```

### Payload Contents

```json
{
  "companyName": "Acme Corp",
  "email": "admin@acme.com",
  "module": "MultiTenancy",
  "issuedAt": "2025-12-01",
  "updatesExpireAt": "2026-12-01",  // 1 year of updates
  "licenseType": "standard",        // standard, enterprise
  "maxProjects": 999,               // Unlimited
  "version": "1.0"
}
```

### Security Features

1. **RSA Signature** - Payload signed with private key, verified with public key
2. **Base64 Encoding** - Human-readable but tamper-proof
3. **Expiration Tracking** - For update eligibility, not usage rights
4. **Email Binding** - Optional: bind to customer email
5. **Domain Binding** - Optional: bind to customer domain (for Enterprise tier)

---

## 💻 CLI Integration

### Adding Pro Modules

**Command**:
```bash
# User already purchased license, has key
netmx add module MultiTenancy --license NETMX-MT-ABC123-XYZ789-...

# Alternative: Prompt for license key
netmx add module MultiTenancy
# CLI prompts: "MultiTenancy is a Pro module ($299). Enter license key:"
```

**CLI Workflow**:
1. Detect module is Pro (`module.json` has `"isPro": true`)
2. Check if `--license` flag provided
3. If not, prompt user to enter license key
4. Validate license key format (not full validation - that's at runtime)
5. Copy module source code (same as free modules)
6. Add license key to `appsettings.json` automatically
7. Inform user about license validation at startup

**CLI Output**:
```
$ netmx add module MultiTenancy --license NETMX-MT-ABC123-...

✅ License key validated (format check)
📦 Copying MultiTenancy module source code...
   ├─ MultiTenancy.Core
   ├─ MultiTenancy.Application
   └─ MultiTenancy.Web
✅ Module added to solution
✅ License key added to appsettings.json
✅ Services registered in Program.cs

⚠️  IMPORTANT: License will be validated at application startup.
   Licensed to: Acme Corp
   Updates valid until: 2026-12-01

   Need help? Visit https://netmx.dev/docs/licensing
```

---

## 🛒 Purchase Flow

### Customer Journey

1. **Browse Module** - Customer views Multi-Tenancy module on netmx.dev
2. **Purchase** - One-time payment ($299) via Stripe
3. **License Generation** - System generates unique license key
4. **Email Delivery** - License key emailed to customer
5. **CLI Installation** - Customer runs `netmx add module MultiTenancy --license KEY`
6. **Runtime Validation** - License validated at application startup
7. **Development** - Customer can customize source code freely

### Self-Service Portal (Future)

**URL**: https://portal.netmx.dev

**Features**:
- View purchased licenses
- Download license keys
- Manage license assignments (Enterprise tier)
- Renew update subscriptions
- View invoice history
- Access support tickets

---

## 📋 License Types

### 1. Standard License ($299)
- 1 license key
- Valid for 1 company/organization
- Unlimited projects within organization
- Unlimited developers within organization
- Source code access
- 1 year of updates included
- Community support (forum, GitHub issues)

### 2. Enterprise License ($1,499)
- 5 license keys (can assign to multiple companies)
- Ideal for agencies building for clients
- Unlimited projects per key
- Unlimited developers per key
- Source code access
- 1 year of updates included
- Priority support (email, 24-hour response)
- Domain binding (optional - restrict to specific domains)

### 3. Freelancer License ($149)
- 1 license key
- Valid for 1 freelancer/solo developer
- Up to 3 client projects
- Source code access
- 1 year of updates included
- Community support

---

## 🔄 Update Renewals

### How It Works

**After 1 Year**:
- License **never expires** for usage (perpetual)
- Updates **stop** after 1 year (no new features/fixes)
- Customer can **optionally renew** for $49/year to continue receiving updates

**Customer Options**:
1. **Continue Using** - Keep using version they have (no additional cost)
2. **Renew Updates** - Pay $49/year for continued updates
3. **Upgrade Tier** - Upgrade from Standard → Enterprise (pay difference)

**CLI Notification**:
```bash
$ netmx update modules

⚠️  MultiTenancy: Update available (v1.5.0), but your updates expired on 2026-12-01.

   Current version: 1.2.0
   Latest version:  1.5.0

   Options:
   1. Continue using v1.2.0 (FREE)
   2. Renew updates for $49/year: https://netmx.dev/renew
   3. Manually download v1.5.0 and replace source code

   Your usage license is perpetual - you can use v1.2.0 forever.
```

---

## 🚫 Anti-Piracy Measures

### What We Do

1. **License Validation** - Required at startup (can't run without valid key)
2. **Signature Verification** - RSA prevents tampering
3. **Audit Logging** - Track license usage (optional telemetry)
4. **Rate Limiting** - Limit license validation requests (prevent brute force)

### What We DON'T Do

❌ **Obfuscation** - Code is readable (customer owns it)  
❌ **Phone-Home** - No required internet connection after initial validation  
❌ **Activation Limits** - Unlimited dev machines  
❌ **Time Bombs** - License never expires for usage  

### Trust Over Enforcement

**Philosophy**: We trust our customers. Source code access is valuable, and we believe developers will pay for quality modules rather than risk piracy.

**If Piracy Detected**:
- Contact customer directly (not public shaming)
- Offer legitimate license at discount
- Invalidate pirated keys
- Legal action only for egregious cases (large companies)

---

## 📊 Telemetry (Optional, Opt-In)

### Anonymous Usage Data

**Collected** (if customer opts in):
- Module name & version
- License key (hashed)
- .NET version
- OS type
- Application start count (for usage stats)

**NOT Collected**:
- Source code
- Business data
- User information
- IP addresses (beyond country-level)

**Opt-Out**:
```json
{
  "NetMX": {
    "Licensing": {
      "Telemetry": false  // Default: false (opt-in, not opt-out)
    }
  }
}
```

---

## 🧪 Testing Without License (Development)

### Trial Mode

**For Evaluation** (14 days):
```bash
$ netmx add module MultiTenancy --trial

⚠️  Trial mode activated (14 days remaining)
   Purchase license: https://netmx.dev/pricing
```

**Trial Limitations**:
- 14-day trial period
- Full functionality
- Watermark in UI: "MultiTenancy Trial - 12 days remaining"
- Reminder emails at 7 days, 3 days, 1 day

### Unit Testing

**For Automated Tests**:
```csharp
// Test projects can mock license validator
services.AddSingleton<ILicenseValidator>(new MockLicenseValidator());

// Or use a test license key (provided in module README)
{
  "NetMX": {
    "Licensing": {
      "MultiTenancy": {
        "LicenseKey": "NETMX-MT-TEST-KEY-FOR-UNIT-TESTS-ONLY"
      }
    }
  }
}
```

---

## 🔧 Implementation Checklist

### For Each Pro Module

- [ ] Add `"isPro": true` to `module.json`
- [ ] Create `LicenseValidation/` folder in `.Core` project
- [ ] Implement `ILicenseValidator` interface
- [ ] Add license check to `AddXXX()` extension method
- [ ] Add clear error messages for missing/invalid licenses
- [ ] Document license setup in module README
- [ ] Create trial mode (14 days)
- [ ] Add telemetry (optional, opt-in)
- [ ] Update CLI to handle Pro modules
- [ ] Generate test license keys for unit tests

---

## 📖 Documentation Requirements

### Module README (Pro)

Every Pro module README must include:

```markdown
## 💰 Licensing

MultiTenancy is a **Pro module** ($299 one-time purchase).

### Purchase
Visit [netmx.dev/pricing](https://netmx.dev/pricing) to purchase a license.

### Installation
```bash
netmx add module MultiTenancy --license YOUR-LICENSE-KEY
```

### Configuration
Add your license key to `appsettings.json`:
```json
{
  "NetMX": {
    "Licensing": {
      "MultiTenancy": {
        "LicenseKey": "NETMX-MT-...",
        "LicensedTo": "Your Company"
      }
    }
  }
}
```

### Trial Mode
Try MultiTenancy FREE for 14 days:
```bash
netmx add module MultiTenancy --trial
```

### Support
- Documentation: https://netmx.dev/docs/multitenancy
- Forum: https://forum.netmx.dev
- Email: support@netmx.dev
```

---

## 🎯 Goals & Success Metrics

### Phase 2 (Multi-Tenancy Launch)

**Target**: Dec 2025 - Jan 2026

**Goals**:
- ✅ License validation working (startup time < 100ms)
- ✅ 10 paying customers (validation of model)
- ✅ Zero license piracy reports
- ✅ Average support ticket resolution < 24 hours
- ✅ Customer satisfaction > 4.5/5

### Phase 3 (Multiple Pro Modules)

**Target**: Jan - Mar 2026

**Goals**:
- ✅ 6 Pro modules launched
- ✅ 50 paying customers
- ✅ $25K revenue
- ✅ License renewal rate > 70% (after 1 year)

---

## ❓ FAQ

**Q: Why source code vs NuGet?**  
A: Transparency, debugging, customization. Developers hate "black boxes."

**Q: Why not just use NuGet private feeds?**  
A: Still requires NuGet authentication, harder to customize, not true "ownership."

**Q: Can customers remove the license check?**  
A: Technically yes (they have source code), but that violates license terms.

**Q: What if license validation fails in production?**  
A: Graceful degradation - log error, disable Pro features, continue running (don't crash app).

**Q: Can customers share license keys?**  
A: No - one license per company/organization. Enterprise tier allows multiple keys for agencies.

**Q: What if a customer's license expires?**  
A: License never expires for **usage** (perpetual). Updates expire after 1 year (optional $49 renewal).

**Q: How do you prevent license key sharing?**  
A: We don't aggressively prevent it. We trust developers and focus on value. Enterprise tier has domain binding for agencies needing stricter control.

**Q: Can customers redistribute Pro modules?**  
A: No - license is per project, not per distribution. Customers can't sell/share Pro module code.

---

## 🚀 Next Steps

### Immediate (Before Multi-Tenancy Launch)

1. **Implement License Validation Library** (1 week)
   - Create `NetMX.Licensing` package
   - Implement RSA signing/verification
   - Build CLI integration
   - Write tests (100% coverage)

2. **Build License Generation System** (1 week)
   - Admin portal for generating keys
   - Stripe integration
   - Email delivery
   - Database for tracking licenses

3. **Update CLI** (3 days)
   - Detect Pro modules
   - Prompt for license key
   - Validate format
   - Add to appsettings.json

4. **Documentation** (2 days)
   - PRO-MODULE-LICENSING.md (this doc)
   - Update ROADMAP.md
   - Update MASTER-OVERVIEW.md
   - Create customer-facing license docs

### Before Phase 3 (Multiple Pro Modules)

1. Self-service portal (netmx.dev/portal)
2. License renewal system
3. Analytics dashboard (license usage)
4. Support ticket system

---

## 📚 Related Documents

- [MASTER-OVERVIEW.md](MASTER-OVERVIEW.md) - Product overview
- [ROADMAP.md](ROADMAP.md) - Development roadmap
- [THE-PRODUCT.md](THE-PRODUCT.md) - Product vision
- [STUDIO-SUITE-VISION.md](STUDIO-SUITE-VISION.md) - Visual tools strategy

---

**Status**: Ready for implementation (Multi-Tenancy module, Dec 2025)  
**Owner**: Development Team  
**Review Cycle**: After each Pro module launch
