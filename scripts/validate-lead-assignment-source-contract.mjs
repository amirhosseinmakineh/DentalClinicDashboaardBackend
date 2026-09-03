import { readFileSync } from "node:fs";

const read = (path) => readFileSync(new URL(`../${path}`, import.meta.url), "utf8");
const requireText = (content, expected, label) => {
  if (!content.includes(expected)) throw new Error(`Missing ${label}: ${expected}`);
};

const repository = read("DentalDashboard.Infrastracture/Repository/LeadAssignmentRepository.cs");
const controller = read("DentalDashboard/Controllers/AdminLeadAssignmentSettingsController.cs");
const pickup = read("DentalDashboard.ApplicationService/Services/PickUpService.cs");
const migration = read("DentalDashboard.Infrastracture/Migrations/20260903120000_AddLeadAssignmentSourceSettings.cs");

requireText(controller, '[Authorize(Roles = "Admin")]', "admin authorization");
requireText(controller, '[Route("api/admin/lead-assignment-settings")]', "settings route");
requireText(repository, "(x.IsDeleted && x.ConsultantProfileId == null)", "deleted burned lead filter");
requireText(repository, "x.LeadAssignmentState == LeadAssignmentState.Pending", "pending reassignment filter");
requireText(repository, "IsolationLevel.Serializable", "serializable pickup transaction");
requireText(repository, "LeadAssignmentHistories.AddAsync", "assignment history write");
requireText(repository, "ConsultantProfileId <> @consultantProfileId", "same-consultant reassignment guard");
requireText(pickup, "Lead assignment succeeded", "successful assignment logging");
requireText(migration, 'name: "LeadAssignmentSettings"', "settings migration");
requireText(migration, 'name: "LeadAssignmentHistories"', "history migration");
requireText(migration, "columnTypes: new[]", "designer-independent seed column types");

console.log("Lead assignment source backend contract validation passed.");
