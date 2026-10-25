import { readFileSync } from "node:fs";

const read = (path) => readFileSync(new URL(`../${path}`, import.meta.url), "utf8");
const requireText = (content, expected, label) => {
  if (!content.includes(expected)) throw new Error(`Missing ${label}: ${expected}`);
};

const repository = read("DentalDashboard.Infrastracture/Repository/PatientFileRepository.cs");
const handler = read("DentalDashboard.ApplicationService/Secretary/PatientFiles/PatientFileHandlers.cs");

requireText(repository, "new PersianCalendar()", "Persian attendance-date prefix");
requireText(repository, "* 1_000_000L", "year component");
requireText(repository, "* 10_000L", "month component");
requireText(repository, "* 100L", "day component");
requireText(repository, "WITH (UPDLOCK, HOLDLOCK)", "concurrent daily allocation lock");
requireText(repository, "BETWEEN @firstNumberOfDay AND @lastNumberOfDay", "daily sequence range");
requireText(handler, "OrderByDescending(reservation => reservation.ReservationAt)", "latest valid attendance reservation");
requireText(handler, "DateOnly.FromDateTime(attendanceAt.Value)", "attendance date allocation input");

console.log("Patient file number contract validation passed.");
