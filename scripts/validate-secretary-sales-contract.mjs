import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const read = (path) => readFileSync(resolve(process.cwd(), path), "utf8");
const commands = read("DentalDashboard.ApplicationService.Contract/Secretary/Accountant/SecretarySales/Commands/SecretarySaleCommands.cs");
const handlers = read("DentalDashboard.ApplicationService/Secretary/Accountant/SecretarySales/Handlers/SecretarySaleCommandHandlers.cs");
const config = read("DentalDashboard.Infrastracture/Secretary/Accountant/SecretarySales/Configurations/SecretarySalesConfigurations.cs");
const unitOfWork = read("DentalDashboard.Infrastracture/Repository/UnitOfWork.cs");

const createSale = commands.match(/class CreateSecretarySaleCommand[\s\S]*?^}/m)?.[0] ?? "";
const assertions = [
  [!createSale.includes("SalePrice") && !createSale.includes("SecretaryReward"), "CreateSecretarySaleCommand must not accept price or reward"],
  [handlers.includes("SalePrice = service.Price") && handlers.includes("SecretaryReward = service.SecretaryReward"), "sale must snapshot price and reward from service"],
  [handlers.includes("Status = SecretarySaleStatus.PendingAdminApproval"), "new sale must be pending"],
  [handlers.includes("IsolationLevel.Serializable"), "sale review must use a serializable transaction"],
  [handlers.includes("wallet.Balance += sale.SecretaryReward"), "approval must credit the snapshotted reward"],
  [config.includes("SecretarySaleId, x.TransactionType") && config.includes("IsUnique()"), "reward transaction must have a unique sale constraint"],
  [unitOfWork.includes("BeginTransactionAsync(isolationLevel, cancellationToken)"), "unit of work must honor requested isolation level"],
];

const failed = assertions.filter(([ok]) => !ok);
if (failed.length) {
  failed.forEach(([, message]) => console.error(`FAIL: ${message}`));
  process.exit(1);
}
console.log("OK: secretary sale snapshots, atomic review, and duplicate-reward guards are present");
