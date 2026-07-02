import { CommonModule } from "@angular/common";
import {
  Component,
  EventEmitter,
  OnInit,
  Output,
  inject,
} from "@angular/core";
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { finalize } from "rxjs";
import { ToastrService } from "ngx-toastr";

import {
  AdminRole,
  RoleCommandPayload,
} from "../../models/admin-management.model";
import {
  createPaginatedResult,
  PaginatedResult,
} from "../../models/paginated-result.model";
import { AdminManagementService } from "../../services/admin-management.service";

export interface RoleRow {
  id: number | string;
  roleName: string;
}

type RoleDialogMode = "create" | "edit" | "delete";
type ExportFormat = "excel" | "pdf";

@Component({
  selector: "app-role-management",
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: "./role-management.component.html",
  styleUrl: "./role-management.component.scss",
})
export class RoleManagementComponent implements OnInit {
  @Output() rolesChanged = new EventEmitter<RoleRow[]>();

  isRolesLoading = false;
  roleLoadError = "";
  isDialogSubmitting = false;
  rolePageNumber = 1;
  rolePageSize = 3;
  readonly pageSizeOptions = [3, 5, 10];

  selectedRole: RoleRow | null = null;
  dialogMode: RoleDialogMode | null = null;
  roles: RoleRow[] = [];

  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly adminManagementService = inject(AdminManagementService);
  private readonly toastr = inject(ToastrService);

  readonly roleForm = this.formBuilder.group({
    roleName: ["", [Validators.required, Validators.maxLength(80)]],
  });

  ngOnInit(): void {
    this.loadRoles();
  }

  get rolePage(): PaginatedResult<RoleRow> {
    return createPaginatedResult(
      this.roles,
      this.rolePageNumber,
      this.rolePageSize,
    );
  }

  get dialogTitle(): string {
    if (this.dialogMode === "create") {
      return "ایجاد نقش جدید";
    }

    if (this.dialogMode === "edit") {
      return "ویرایش نقش";
    }

    if (this.dialogMode === "delete") {
      return "حذف نقش";
    }

    return "";
  }

  openRoleDialog(mode: RoleDialogMode, role?: RoleRow): void {
    this.dialogMode = mode;
    this.selectedRole = role ?? null;

    if (mode === "create") {
      this.roleForm.reset({ roleName: "" });
      return;
    }

    if (role) {
      this.roleForm.reset({ roleName: role.roleName });
    }
  }

  closeDialog(): void {
    this.dialogMode = null;
    this.selectedRole = null;
  }

  submitRoleDialog(): void {
    if (this.dialogMode !== "delete" && this.roleForm.invalid) {
      this.roleForm.markAllAsTouched();
      return;
    }

    if (this.isDialogSubmitting) {
      return;
    }

    if (this.dialogMode === "delete" && this.selectedRole) {
      this.isDialogSubmitting = true;

      this.adminManagementService
        .deleteRole(this.toRoleCommand(this.selectedRole))
        .pipe(finalize(() => (this.isDialogSubmitting = false)))
        .subscribe((result) => {
          if (!result.isSuccess) {
            this.toastr.error(result.message || "حذف نقش ناموفق بود.");
            return;
          }

          this.roles = this.roles.filter(
            (role) => role.id !== this.selectedRole?.id,
          );
          this.rolePageNumber = Math.min(
            this.rolePageNumber,
            Math.max(1, Math.ceil(this.roles.length / this.rolePageSize)),
          );
          this.toastr.success(result.message || "نقش حذف شد.");
          this.closeDialog();
          this.emitRoles();
          this.loadRoles(false);
        });
      return;
    }

    const roleName = this.roleForm.controls.roleName.value.trim();
    const command: RoleCommandPayload = this.selectedRole
      ? { id: this.selectedRole.id, roleName }
      : { roleName };
    const request$ =
      this.dialogMode === "edit"
        ? this.adminManagementService.updateRole(command)
        : this.adminManagementService.createRole(command);

    this.isDialogSubmitting = true;
    request$
      .pipe(finalize(() => (this.isDialogSubmitting = false)))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.toastr.error(result.message || "ثبت اطلاعات نقش ناموفق بود.");
          return;
        }

        if (this.dialogMode === "edit" && this.selectedRole) {
          this.roles = this.roles.map((role) =>
            role.id === this.selectedRole?.id ? { ...role, roleName } : role,
          );
        }

        this.rolePageNumber = 1;
        this.toastr.success(
          result.message ||
            (this.dialogMode === "edit"
              ? "نقش به‌روزرسانی شد."
              : "نقش جدید ایجاد شد."),
        );
        this.closeDialog();
        this.emitRoles();
        this.loadRoles(false);
      });
  }

  changeRolePage(pageNumber: number): void {
    this.rolePageNumber = pageNumber;
  }

  changeRolePageSize(event: Event): void {
    this.rolePageSize = Number((event.target as HTMLSelectElement).value);
    this.rolePageNumber = 1;
  }

  exportRoles(format: ExportFormat): void {
    this.exportRows(
      format,
      "roles",
      ["شناسه", "نام نقش"],
      this.roles.map((role) => [`${role.id}`, role.roleName]),
    );
  }

  trackById(_index: number, item: { id: number | string }): number | string {
    return item.id;
  }

  trackByValue(_index: number, value: number | string): number | string {
    return value;
  }

  private loadRoles(showLoading = true): void {
    if (showLoading) {
      this.isRolesLoading = true;
    }

    this.adminManagementService
      .getRoles()
      .pipe(finalize(() => (this.isRolesLoading = false)))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.roleLoadError =
            result.message ||
            "امکان دریافت نقش‌ها از API پورت ۵۱۸۲ وجود ندارد.";
          this.toastr.error(this.roleLoadError);
          this.emitRoles();
          return;
        }

        this.roleLoadError = "";
        this.roles = (result.data ?? []).map((role, index) =>
          this.toRoleRow(role, index),
        );
        this.rolePageNumber = 1;
        this.emitRoles();
      });
  }

  private toRoleRow(role: AdminRole, index: number): RoleRow {
    return {
      id: role.id ?? role.roleId ?? index + 1,
      roleName: role.roleName?.trim() || "نقش بدون نام",
    };
  }

  private toRoleCommand(role: RoleRow): RoleCommandPayload {
    return {
      id: role.id,
      roleName: role.roleName,
    };
  }

  private exportRows(
    format: ExportFormat,
    fileName: string,
    headers: string[],
    rows: string[][],
  ): void {
    if (format === "excel") {
      const csv = [headers, ...rows]
        .map((row) =>
          row.map((cell) => `"${cell.replace(/"/g, '\"\"')}"`).join(","),
        )
        .join("\n");
      this.downloadFile(
        `${fileName}.csv`,
        `\ufeff${csv}`,
        "text/csv;charset=utf-8;",
      );
      return;
    }

    const tableRows = rows
      .map(
        (row) =>
          `<tr>${row.map((cell) => `<td>${this.escapeHtml(cell)}</td>`).join("")}</tr>`,
      )
      .join("");
    const html = `<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><title>${fileName}</title><style>body{font-family:tahoma,sans-serif;padding:24px}table{width:100%;border-collapse:collapse}th,td{border:1px solid #ddd;padding:10px;text-align:right}</style></head><body><h1>${fileName}</h1><table><thead><tr>${headers.map((header) => `<th>${this.escapeHtml(header)}</th>`).join("")}</tr></thead><tbody>${tableRows}</tbody></table></body></html>`;
    const printWindow = window.open("", "_blank", "noopener,noreferrer");

    if (!printWindow) {
      this.downloadFile(`${fileName}.html`, html, "text/html;charset=utf-8;");
      return;
    }

    printWindow.document.write(html);
    printWindow.document.close();
    printWindow.focus();
    printWindow.print();
  }

  private downloadFile(fileName: string, content: string, type: string): void {
    const blob = new Blob([content], { type });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  private escapeHtml(value: string): string {
    return value.replace(
      /[&<>"]/g,
      (char) =>
        ({
          "&": "&amp;",
          "<": "&lt;",
          ">": "&gt;",
          '"': "&quot;",
        })[char] ?? char,
    );
  }

  private emitRoles(): void {
    this.rolesChanged.emit(this.roles);
  }
}
