# Incident: [Short, Punchy Description of the Error]

- **Date**: YYYY-MM-DD
- **Service Affected**: `orders-service` / `Orders.Api`
- **Environment**: Local Development
- **Trigger Command**: `dotnet watch run`

## 🚨 Symptom / Error Log
<!-- Paste the exact terminal error or stack trace here -->
```text
dotnet watch ❌ An unexpected error occurred: System.IO.IOException: The configured user limit (128) on the number of inotify instances has been reached...
```

## 🔍 Root Cause
<!-- Why did this happen? Keep it brief. -->
The Linux system's kernel limit for `fs.inotify.max_user_instances` was capped at 128. Running multiple .NET microservices or watching large file trees exhausts this pool, causing `dotnet watch` to crash on startup.

## 🛠️ Resolution / Fix
To fix this crash, you need to increase the Linux inotify user instances limit, which restricts how many directories .NET can watch simultaneously. The vulnerability warning about Microsoft.OpenApi is separate and can be fixed by updating your package.

### Temporary Fix
Run this to increase limits immediately:
```bash
sudo sysctl fs.inotify.max_user_instances=512
```

### Permanent Fix
Append the setting to your system configuration:
```bash
echo "fs.inotify.max_user_instances=512" | sudo tee -a /etc/sysctl.conf
sudo sysctl -p
```

## 💡 Prevention / Notes
<!-- How can we prevent this or automate the fix? -->
- Add this system configuration step to the team's `README.md` onboarding guide for local machine setup.
- Consider configuring development containers (DevContainers) with optimized host limits if the team moves to containerized development.
