import { readFileSync } from "node:fs";

const read = (path) => readFileSync(new URL(`../${path}`, import.meta.url), "utf8");
const requireText = (content, expected, label) => {
  if (!content.includes(expected)) throw new Error(`Missing ${label}: ${expected}`);
};

const repository = read("DentalDashboard.Infrastracture/Repository/LeadAssignmentRepository.cs");
const controller = read("DentalDashboard/Controllers/AdminLeadAssignmentSettingsController.cs");
const pickup = read("DentalDashboard.ApplicationService/Services/PickUpService.cs");
const candidateProvider = read("DentalDashboard.ApplicationService/Services/LeadAssignmentCandidateProvider.cs");
const assignmentService = read("DentalDashboard.ApplicationService/Services/LeadAssignmentService.cs");
const broadcastHandler = read("DentalDashboard.ApplicationService/Handlers/QueryHandlers/Consultant/GetBroadcastRealtimeLeadsQueryHandler.cs");
const broadcastResponse = read("DentalDashboard.ApplicationService.Contract/Responses/ConsultantResponse/BroadcastRealtimeLeadResponse.cs");
const migration = read("DentalDashboard.Infrastracture/Migrations/20260903120000_AddLeadAssignmentSourceSettings.cs");

requireText(controller, '[Authorize(Roles = "Admin")]', "admin authorization");
requireText(controller, '[Route("api/admin/lead-assignment-settings")]', "settings route");
requireText(candidateProvider, "GetSourceTypeAsync", "candidate source setting lookup");
requireText(candidateProvider, "CountAssignmentCandidatesAsync(sourceType", "source-aware candidate count");
requireText(candidateProvider, "GetCurrentRealtimeLeadForDispatchAsync(sourceType", "source-aware candidate selection");
requireText(repository, "if (sourceType == LeadAssignmentSourceType.BurnedLeads)", "candidate query source branch");
requireText(repository, "(x.IsDeleted && x.ConsultantProfileId == null)", "deleted burned lead filter");
requireText(repository, "x.LeadAssignmentState == LeadAssignmentState.Pending", "pending reassignment filter");
requireText(repository, "IsolationLevel.Serializable", "serializable pickup transaction");
requireText(repository, "context.LeadAssignmentSettings", "pickup source setting recheck");
requireText(repository, "LeadAssignmentHistories.AddAsync", "assignment history write");
requireText(repository, "ConsultantProfileId <> @consultantProfileId", "same-consultant reassignment guard");
requireText(repository, "previousUnassignedLeads", "new-lead fallback query");
requireText(repository, "!newLeads.Any()", "fallback only after new candidates are exhausted");
requireText(repository, "NOT EXISTS (", "atomic new-lead priority guard");
requireText(pickup, "Lead assignment succeeded", "successful assignment logging");
requireText(assignmentService, "Yektanet lead request failed", "Yektanet failure fallback logging");
requireText(assignmentService, '"Yektanet:LeadReportUrl"', "external Yektanet URL configuration");
requireText(assignmentService, '["leadLimitType"] = leadLimitType', "push lead source payload");
requireText(assignmentService, '? "لید سوخته"', "burned lead push title");
requireText(broadcastHandler, "LeadLimitType = candidateBatch.SourceType", "polling lead source mapping");
requireText(broadcastResponse, "public string LeadLimitType", "polling lead source contract");
if (assignmentService.includes("landing.yektanet.com/form/report/")) {
  throw new Error("Yektanet report URL must not be hardcoded in source");
}
requireText(migration, 'name: "LeadAssignmentSettings"', "settings migration");
requireText(migration, 'name: "LeadAssignmentHistories"', "history migration");
requireText(migration, "columnTypes: new[]", "designer-independent seed column types");

console.log("Lead assignment source backend contract validation passed.");
