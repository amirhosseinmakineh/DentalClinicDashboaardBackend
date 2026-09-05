#!/usr/bin/env bash
set -euo pipefail

module_root="DentalDashboard.Accounting"

if grep -RInE "DbContext|DentalContext" \
  "$module_root/Application" \
  "$module_root/Controllers" \
  "$module_root/Integration" \
  --include='*.cs'; then
  echo "Accounting application/controller code must not depend on DbContext." >&2
  exit 1
fi

if grep -RInE "namespace DentalDashboard\.(ApplicationService|Domain|Infrastracture|Secretary\.Accountant|Controllers)" \
  "$module_root/Application" \
  "$module_root/Contracts" \
  "$module_root/Controllers" \
  "$module_root/Domain" \
  "$module_root/Infrastructure/Configurations" \
  "$module_root/Infrastructure/PatientFinance" \
  "$module_root/Infrastructure/Repositories" \
  "$module_root/Infrastructure/Registration" \
  "$module_root/Infrastructure/SecretarySales" \
  --include='*.cs'; then
  echo "Accounting code leaked into a legacy namespace." >&2
  exit 1
fi

controller_count=$(find "$module_root/Controllers" -maxdepth 1 -name '*Controller.cs' | wc -l)
if [[ "$controller_count" -ne 5 ]]; then
  echo "Expected exactly five accounting API controllers; found $controller_count." >&2
  exit 1
fi

echo "Accounting module boundaries are valid."
